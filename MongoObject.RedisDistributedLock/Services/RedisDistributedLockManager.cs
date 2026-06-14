using Microsoft.Extensions.Logging;
using MongoObject.Core.Data;
using MongoObject.Core.Exceptions;
using MongoObject.Core.Interfaces;
using StackExchange.Redis;
using System.Diagnostics.CodeAnalysis;

namespace MongoObject.RedisDistributedLock.Services
{
    public class RedisDistributedLockManager(
        IConnectionMultiplexer redis,
        MongoObjectOptions options,
        IDocumentKeyManager keys,
        ILogger<RedisDistributedLockManager> logger) : IDistributedLockManager
    {
        private readonly IDatabase _db = redis.GetDatabase();

        // Lua script for atomic lock acquisition (acquire if free or expired)
        private const string LockAcquireScript = """
            local key = KEYS[1]
            local now = tonumber(ARGV[1])
            local holderId = ARGV[2]
            local expiresAt = ARGV[3]
            local acquiredAt = ARGV[4]
            local ttlSeconds = tonumber(ARGV[5])

            local existing = redis.call('HGET', key, 'expiresAt')
            if existing == false or tonumber(existing) < now then
                redis.call('HSET', key, 'holderId', holderId, 'expiresAt', expiresAt, 'acquiredAt', acquiredAt)
                redis.call('EXPIRE', key, ttlSeconds)
                return 1
            end
            return 0
            """;

        // Lua script for atomic lock release (only release if we own it)
        private const string LockReleaseScript = """
            local key = KEYS[1]
            local holderId = ARGV[1]

            local current = redis.call('HGET', key, 'holderId')
            if current == holderId then
                redis.call('DEL', key)
                return 1
            end
            return 0
            """;

        // Lua script for atomic lock renewal (only renew if we own it and it hasn't expired)
        private const string LockRenewScript = """
            local key = KEYS[1]
            local now = tonumber(ARGV[1])
            local holderId = ARGV[2]
            local newExpiresAt = ARGV[3]
            local ttlSeconds = tonumber(ARGV[4])

            local current = redis.call('HGET', key, 'holderId')
            local expiresAt = tonumber(redis.call('HGET', key, 'expiresAt') or '0')

            if current == holderId and expiresAt > now then
                redis.call('HSET', key, 'expiresAt', newExpiresAt)
                redis.call('EXPIRE', key, ttlSeconds)
                return 1
            end
            return 0
            """;

        private static string BuildLockKey(string documentKey) => $"mongolock:{documentKey}";

        public async Task<LockAcquisitionResult> LockDocumentAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(
            T document, TimeSpan? duration = null)
            where T : class, IDocumentFile, new()
        {
            if (!keys.TryGetKey(document, out string? key))
            {
                throw new InvalidOperationException("Cannot lock untracked Document");
            }

            var holderId = GenerateHolderId(key!);
            var now = DateTimeOffset.UtcNow;
            var lockDuration = duration ?? options.DistributedLockDefaultLockDuration;
            var expiresAt = now.Add(lockDuration);

            var redisKey = BuildLockKey(key!);
            var nowMs = now.ToUnixTimeMilliseconds();
            var expiresAtMs = expiresAt.ToUnixTimeMilliseconds();
            var acquiredAtMs = nowMs;
            var ttlSeconds = (long)Math.Ceiling(lockDuration.TotalSeconds);

            try
            {
                var result = (long)await _db.ScriptEvaluateAsync(
                    LockAcquireScript,
                    new RedisKey[] { redisKey },
                    new RedisValue[] { nowMs, holderId, expiresAtMs, acquiredAtMs, ttlSeconds });

                if (result == 1)
                {
                    logger.LogDebug("Lock acquired for key '{Key}' by holder '{HolderId}'", key, holderId);
                    return LockAcquisitionResult.Success(holderId, expiresAt.UtcDateTime);
                }

                // Lock is held by someone else - get current lock info
                var current = await GetLock(key!);
                var msg = current?.LockedBy != null
                    ? $"Locked by '{current.LockedBy}' until {current.LockExpiresAt}"
                    : "Failed to acquire lock";

                logger.LogDebug("Failed to acquire lock for key '{Key}': {Message}", key, msg);
                return LockAcquisitionResult.Failed(msg, current?.LockedBy ?? string.Empty, current?.LockExpiresAt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error acquiring lock for key '{Key}'", key);
                return LockAcquisitionResult.Failed($"Failed to acquire lock: {ex.Message}");
            }
        }

        public async Task<LockAcquisitionResult> LockDocumentAsyncNew<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(
            T document, TimeSpan? duration = null)
            where T : class, IDocumentFile, new()
        {
            if (!keys.TryGetKey(document, out string? key))
            {
                throw new InvalidOperationException("Cannot lock untracked Document");
            }

            var current = await GetLock(key!);
            var now = DateTimeOffset.UtcNow;
            var lockDuration = duration ?? options.DistributedLockDefaultLockDuration;
            var expiresAt = now.Add(lockDuration);

            if (current != null && current.LockExpiresAt > now.UtcDateTime)
            {
                return LockAcquisitionResult.Failed(
                    "Acquisition Failed, the document is already locked, if you are trying to extend use RenewLockAsync instead",
                    current.LockedBy ?? string.Empty,
                    current.LockExpiresAt);
            }

            // Either expired lock or no lock - take it over
            var holderId = GenerateHolderId(key!);
            var redisKey = BuildLockKey(key!);
            var nowMs = now.ToUnixTimeMilliseconds();
            var expiresAtMs = expiresAt.ToUnixTimeMilliseconds();
            var ttlSeconds = (long)Math.Ceiling(lockDuration.TotalSeconds);

            try
            {
                var result = (long)await _db.ScriptEvaluateAsync(
                    LockAcquireScript,
                    new RedisKey[] { redisKey },
                    new RedisValue[] { nowMs, holderId, expiresAtMs, nowMs, ttlSeconds });

                if (result == 1)
                {
                    logger.LogDebug("Lock acquired (new) for key '{Key}' by holder '{HolderId}'", key, holderId);
                    return LockAcquisitionResult.Success(holderId, expiresAt.UtcDateTime);
                }

                return LockAcquisitionResult.Failed("Failed to lock document");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error acquiring new lock for key '{Key}'", key);
                return LockAcquisitionResult.Failed($"Failed to acquire lock: {ex.Message}");
            }
        }

        public async Task<bool> IsLocked<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(
            IMongoLockScope? scope, T document)
            where T : class, IDocumentFile, new()
        {
            if (!keys.TryGetKey(document, out var key))
            {
                throw new InvalidOperationException("Cannot check untracked Document");
            }

            var redisKey = BuildLockKey(key!);
            var hashEntries = await _db.HashGetAllAsync(redisKey);

            if (hashEntries.Length == 0)
            {
                return false;
            }

            var lockData = ParseLockMetadata(key!, hashEntries);
            var now = DateTime.UtcNow;

            bool isLockActive = lockData.LockedBy != null && lockData.LockExpiresAt > now;

            if (isLockActive)
            {
                // Active lock - check if it's held by us
                return lockData.LockedBy != scope?.HolderId;
            }

            return false;
        }

        public async Task<LockMetadata?> GetLock(string key)
        {
            var redisKey = BuildLockKey(key);
            var hashEntries = await _db.HashGetAllAsync(redisKey);

            if (hashEntries.Length == 0)
            {
                return null;
            }

            return ParseLockMetadata(key, hashEntries);
        }

        public async Task<LockMetadata?> GetLock(IMongoLockScope recordKey)
        {
            // Redis doesn't support querying by value efficiently.
            // We store the holderId in the hash, but we need to know the key to look it up.
            // The scope's HolderId encodes the document key, so we can extract it.
            // HolderId format: "{MachineName}-{ProcessId}-{documentKey}-{guid}"
            // We'll scan for the lock by checking if the holderId matches.
            // For production use, consider maintaining a reverse index.
            var holderId = recordKey.HolderId;

            // Extract the document key from the holderId
            // Format: "{MachineName}-{ProcessId}-{documentKey}-{guid}"
            var parts = holderId.Split('-');
            if (parts.Length < 4)
            {
                return null;
            }

            // The document key is everything between the ProcessId and the last guid segment
            // MachineName may contain hyphens, ProcessId is numeric, guid is 32 hex chars
            // Find the last 32-char hex segment (guid) and the numeric ProcessId
            var guidPart = parts[^1]; // last segment
            var processIdIndex = -1;
            for (int i = parts.Length - 2; i >= 1; i--)
            {
                if (long.TryParse(parts[i], out _))
                {
                    processIdIndex = i;
                    break;
                }
            }

            if (processIdIndex < 0)
            {
                return null;
            }

            // documentKey is between processIdIndex+1 and parts.Length-2
            var documentKey = string.Join("-", parts[(processIdIndex + 1)..(parts.Length - 1)]);
            if (string.IsNullOrEmpty(documentKey))
            {
                return null;
            }

            var redisKey = BuildLockKey(documentKey);
            var hashEntries = await _db.HashGetAllAsync(redisKey);

            if (hashEntries.Length == 0)
            {
                return null;
            }

            var lockData = ParseLockMetadata(documentKey, hashEntries);

            // Verify this lock is actually held by the requested holder
            if (lockData.LockedBy != holderId)
            {
                return null;
            }

            return lockData;
        }

        public async Task ReleaseLockAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(
            T document, string holderId)
            where T : class, IDocumentFile, new()
        {
            if (!keys.TryGetKey(document, out var key))
            {
                throw new InvalidOperationException("A Lock cannot be released from an untracked document");
            }

            await ReleaseLockAsync<T>(key!, holderId);
        }

        public async Task ReleaseLockAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(
            string recordKey, string holderId)
            where T : class, IDocumentFile, new()
        {
            var redisKey = BuildLockKey(recordKey);

            try
            {
                var result = (long)await _db.ScriptEvaluateAsync(
                    LockReleaseScript,
                    new RedisKey[] { redisKey },
                    new RedisValue[] { holderId });

                if (result == 1)
                {
                    logger.LogDebug("Lock released for key '{Key}' by holder '{HolderId}'", recordKey, holderId);
                }
                else
                {
                    logger.LogWarning("Failed to release lock for key '{Key}' - lock not held by '{HolderId}'", recordKey, holderId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error releasing lock for key '{Key}'", recordKey);
                throw;
            }
        }

        public async Task<IMongoLockScope> LockScopedAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(
            T document, TimeSpan? duration = null)
            where T : class, IDocumentFile, new()
        {
            if (!keys.TryGetKey(document, out string? key))
            {
                throw new InvalidOperationException("Cannot acquire a lock on an untracked document");
            }

            var result = await LockDocumentAsync<T>(document, duration);

            if (result.SuccessResult != LockResult.Success)
            {
                throw new MongoLockAcquisitionException(
                    $"Failed to acquire lock for record '{key}' of type {typeof(T).Name}. " +
                    $"Reason: {result.ErrorMessage}");
            }

            return new MongoRecordLockScope<T>(this, null, key!, result.HolderId!);
        }

        public async Task<bool> RenewLockAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(
            string recordKey, string holderId, TimeSpan? extendBy = null)
            where T : class, IDocumentFile, new()
        {
            var now = DateTimeOffset.UtcNow;
            var extension = extendBy ?? TimeSpan.FromMinutes(10);
            var newExpiresAt = now.Add(extension);

            var redisKey = BuildLockKey(recordKey);
            var nowMs = now.ToUnixTimeMilliseconds();
            var newExpiresAtMs = newExpiresAt.ToUnixTimeMilliseconds();
            var ttlSeconds = (long)Math.Ceiling(extension.TotalSeconds);

            try
            {
                var result = (long)await _db.ScriptEvaluateAsync(
                    LockRenewScript,
                    new RedisKey[] { redisKey },
                    new RedisValue[] { nowMs, holderId, newExpiresAtMs, ttlSeconds });

                if (result == 1)
                {
                    logger.LogDebug("Lock renewed for key '{Key}' by holder '{HolderId}'", recordKey, holderId);
                    return true;
                }

                logger.LogWarning("Failed to renew lock for key '{Key}' - lock not held by '{HolderId}' or expired", recordKey, holderId);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error renewing lock for key '{Key}'", recordKey);
                return false;
            }
        }

        private static LockMetadata ParseLockMetadata(string key, HashEntry[] hashEntries)
        {
            var dict = hashEntries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());

            DateTime? expiresAt = null;
            DateTime? acquiredAt = null;

            if (dict.TryGetValue("expiresAt", out var expiresAtStr) && long.TryParse(expiresAtStr, out var expiresAtMs))
            {
                expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(expiresAtMs).UtcDateTime;
            }

            if (dict.TryGetValue("acquiredAt", out var acquiredAtStr) && long.TryParse(acquiredAtStr, out var acquiredAtMs))
            {
                acquiredAt = DateTimeOffset.FromUnixTimeMilliseconds(acquiredAtMs).UtcDateTime;
            }

            return new LockMetadata
            {
                Id = key,
                LockedBy = dict.TryGetValue("holderId", out var holderId) ? holderId : null,
                LockExpiresAt = expiresAt,
                LockAcquiredAt = acquiredAt
            };
        }

        private static string GenerateHolderId(string key) =>
            $"{Environment.MachineName}-{Environment.ProcessId}-{key}-{Guid.NewGuid():N}";
    }
}
