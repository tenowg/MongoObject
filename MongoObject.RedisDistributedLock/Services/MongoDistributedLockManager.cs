using MongoDB.Driver;
using MongoObject.Core.Data;
using MongoObject.Core.Exceptions;
using MongoObject.Core.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace MongoObject.RedisDistributedLock.Services
{
    public class MongoDistributedLockManager(IMongoClient client, MongoObjectOptions options, IDocumentKeyManager keys) : IDistributedLockManager
    {
        private IMongoCollection<LockMetadata> _lockCollection = client.GetDatabase(options.MongoSystemDatabaseName).GetCollection<LockMetadata>(options.DistributedLockCollectionName);
        private FilterDefinitionBuilder<LockMetadata> _filterDefinition = Builders<LockMetadata>.Filter;
        private UpdateDefinitionBuilder<LockMetadata> _updateDefinition = Builders<LockMetadata>.Update;

        public async Task<LockAcquisitionResult> LockDocumentAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(T document, TimeSpan? duration = null)
            where T : class, IDocumentFile, new()
        {
            if (!keys.TryGetKey(document, out string? key))
            {
                throw new InvalidOperationException("Cannot lock untracked Document");
            }

            var holderId = GenerateHolderId(key!);

            var now = DateTime.UtcNow;
            var expiresAt = now.Add(duration ?? options.DistributedLockDefaultLockDuration);

            var filter = _filterDefinition.And(
                _filterDefinition.Eq(x => x.Id, key!),
                _filterDefinition.Or(
                    _filterDefinition.Eq(x => x.LockedBy, null),
                    _filterDefinition.Lt(x => x.LockExpiresAt, now)
                )
            );
            var update = _updateDefinition
                .Set(x => x.LockedBy, holderId)
                .Set(x => x.LockExpiresAt, expiresAt)
                .Set(x => x.LockAcquiredAt, now);

            var findOptions = new FindOneAndUpdateOptions<LockMetadata>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = false
            };

            var result = await _lockCollection.FindOneAndUpdateAsync(filter, update, findOptions);

            if (result != null)
            {
                return LockAcquisitionResult.Success(holderId, expiresAt);
            }

            var current = await GetLock(key!);

            var data = new LockMetadata
            {
                Id = key!,
                LockAcquiredAt = now,
                LockExpiresAt = expiresAt,
                LockedBy = holderId
            };

            try
            {
                if (current == null)
                {
                    // if it is null, there is no lock, so we lock it
                    await _lockCollection.InsertOneAsync(data);

                    return LockAcquisitionResult.Success(holderId, expiresAt);
                }
            }
            catch (Exception)
            {
                return LockAcquisitionResult.Failed("Failed initilizing Lock on the database");
            }

            var msg = current?.LockedBy != null
                ? $"Locked by '{current.LockedBy}' until {current.LockExpiresAt}"
                : "Failed to acquire lock (record may not exist)";

            return LockAcquisitionResult.Failed(msg, current?.LockedBy ?? string.Empty, current?.LockExpiresAt);
        }

        public async Task<LockAcquisitionResult> LockDocumentAsyncNew<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(T document, TimeSpan? duration = null)
            where T : class, IDocumentFile, new()
        {
            if (!keys.TryGetKey(document, out string? key))
            {
                throw new InvalidOperationException("Cannot lock untracked Document");
            }

            var current = await GetLock(key!);
            var now = DateTime.UtcNow;
            var expiresAt = now.Add(duration ?? options.DistributedLockDefaultLockDuration);

            if (current != null)
            {
                // we are locked by someone
                // to update the duration is irrelevant because that is not this methods job, user RenewLockAsync unless this lock is expired
                if (current.LockExpiresAt > now)
                {
                    return LockAcquisitionResult.Failed("Acquisition Failed, the document is already locked, if you are trying to extend use RenewLockAsync instead", current?.LockedBy ?? string.Empty, current?.LockExpiresAt);
                }


                // either expired lock or no lock
                var holderId = GenerateHolderId(key!);

                var filter = _filterDefinition.Eq(x => x.Id, key!);

                var update = _updateDefinition
                    .Set(x => x.LockedBy, holderId)
                    .Set(x => x.LockExpiresAt, expiresAt)
                    .Set(x => x.LockAcquiredAt, now);

                var updateOptions = new UpdateOptions
                {
                    IsUpsert = false
                };

                // we will take over an expired lock, or create a new one
                var result = await _lockCollection.UpdateOneAsync(filter, update, updateOptions);

                if (result.ModifiedCount > 0)
                {
                    return LockAcquisitionResult.Success(holderId, expiresAt);
                }
            }
            return LockAcquisitionResult.Failed("Failed to lock document");
        }

        public async Task<bool> IsLocked<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(IMongoLockScope? scope, T document) where T : class, IDocumentFile, new()
        {
            if (!keys.TryGetKey(document, out var key))
            {
                throw new InvalidOperationException("Cannot check untracked Document");
            }

            // 1. Fetch the raw lock record directly by ID (no conditional filters)
            var lockData = await _lockCollection.Find(x => x.Id == key).FirstOrDefaultAsync();

            // 2. If no lock record exists at all, it's wide open
            if (lockData == null)
            {
                return false;
            }

            var now = DateTime.UtcNow;

            // 3. Check if the lock is fundamentally active
            bool isLockActive = lockData.LockedBy != null && lockData.LockExpiresAt > now;

            if (isLockActive)
            {
                // It's active, but is it held by US? If so, we are NOT locked out.
                return lockData.LockedBy != scope?.HolderId;
            }

            // Lock is either null or expired
            return false;
        }

        public async Task<LockMetadata?> GetLock(string key)
        {
            var filter = _filterDefinition.Eq(x => x.Id, key);

            var projection = Builders<LockMetadata>.Projection
                .Include(x => x.LockedBy)
                .Include(x => x.LockExpiresAt)
                .Include(x => x.LockAcquiredAt);

            var result = await _lockCollection
                .Find(filter)
                .Project<LockMetadata>(projection)
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<LockMetadata?> GetLock(IMongoLockScope recordKey)
        {
            var filter = _filterDefinition.Eq(x => x.LockedBy, recordKey.HolderId);

            var projection = Builders<LockMetadata>.Projection
                .Include(x => x.LockedBy)
                .Include(x => x.LockExpiresAt)
                .Include(x => x.LockAcquiredAt);

            var result = await _lockCollection
                .Find(filter)
                .Project<LockMetadata>(projection)
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task ReleaseLockAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(T document, string holderId) where T : class, IDocumentFile, new()
        {
            if (!keys.TryGetKey(document, out var key))
            {
                throw new InvalidOperationException("A Lock cannot be released from a untracked document");
            }

            GenerateHolderId(key!);
        }

        public async Task ReleaseLockAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string recordKey, string holderId) where T : class, IDocumentFile, new()
        {
            //var connection = sp.GetRequiredService<IMongoConnection<T>>();
            //var collection = connection.Collection ?? throw new Exception($"MongoCollection for {typeof(T).Name} is null");

            var filter = _filterDefinition.And(
                _filterDefinition.Eq(x => x.Id, recordKey),
                _filterDefinition.Eq(x => x.LockedBy, holderId)
            );

            //var update = _updateDefinition
            //    .Set(x => x.LockedBy, null)
            //    .Set(x => x.LockExpiresAt, null)
            //    .Set(x => x.LockAcquiredAt, null);

            await _lockCollection.DeleteOneAsync(filter);
            //await _lockCollection.UpdateOneAsync(filter, update);
        }

        /// <summary>
        /// Acquires a lock and returns a scope that automatically releases it when disposed.
        /// Recommended for most use cases (cleanest syntax).
        /// </summary>
        public async Task<IMongoLockScope> LockScopedAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(
            T document,
            TimeSpan? duration = null)
            where T : class, IDocumentFile, new()
        {
            if (!keys.TryGetKey(document, out string? key))
            {
                throw new InvalidOperationException("Cannot aquire a lock on a untracked document");
            }

            var result = await LockDocumentAsync<T>(document, duration);

            if (!result.SuccessResult)
            {
                // General-purpose exception (not chat-specific)
                throw new MongoLockAcquisitionException(
                    $"Failed to acquire lock for record '{key}' of type {typeof(T).Name}. " +
                    $"Reason: {result.ErrorMessage}");
            }

            return new MongoRecordLockScope<T>(this, null, key!, result.HolderId!);
        }

        public async Task<bool> RenewLockAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string recordKey, string holderId, TimeSpan? extendBy = null) where T : class, IDocumentFile, new()
        {
            var now = DateTime.UtcNow;
            var expiresAt = now.Add(extendBy ?? TimeSpan.FromMinutes(10));

            var filter = _filterDefinition.And(
                _filterDefinition.Eq(x => x.Id, recordKey),
                _filterDefinition.Eq(x => x.LockedBy, holderId),
                _filterDefinition.Gt(x => x.LockExpiresAt, now)
            );
            var update = _updateDefinition
                .Set(x => x.LockedBy, holderId)
                .Set(x => x.LockExpiresAt, expiresAt)
                .Set(x => x.LockAcquiredAt, now);

            var options = new FindOneAndUpdateOptions<LockMetadata>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = false
            };

            var result = await _lockCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        private static string GenerateHolderId(string key) =>
            $"{Environment.MachineName}-{Environment.ProcessId}-{key}-{Guid.NewGuid():N}";
    }
}
