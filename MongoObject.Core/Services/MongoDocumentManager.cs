using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;
using MongoOptions.Services;
using System.Linq.Expressions;

namespace MongoObject.Core.Services
{
    internal class MongoDocumentManager<T>(IMongoConnection<T> connection,
                                           IDocumentKeyManager keyManager,
                                           IDistributedLockManager lockManager,
                                           InternalCacheService cache,
                                           IMongoClient client,
                                           MongoServerCapabilities capabilities,
                                           MongoObjectOptions options)
        where T : class, IDocumentFile, new()
    {
        private bool? _encrypted = null;

        private readonly MemoryCacheEntryOptions cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = options.CacheHardDuration,
            SlidingExpiration = options.CacheSoftDuration
        };
        private bool isTrackable = typeof(IDocumentFileInternal).IsAssignableFrom(typeof(T));

        public async Task<string> AddDocument<TMetaBase>(T document, Action<TMetaBase>? action, CancellationToken cancellationToken = default)
            where TMetaBase : class, IMetadataBase, new()
        {
            var meta = new TMetaBase
            {
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow,
                Version = 1
            };
            action?.Invoke(meta);

            var mongoDocument = new MongoDocument<T>
            {
                Document = document,
                Metadata = meta.ToBsonDocument()
            };

            var collection = connection.Collection;

            await collection.InsertOneAsync(mongoDocument, null, cancellationToken);

            document.Version = meta.Version.Value;
            var key = keyManager.SetKey(mongoDocument);
            cache.Add(key, mongoDocument, cacheOptions);

            return key;
        }

        public async Task<IEnumerable<T>> ClassSearch<TClassSearch>(Action<TClassSearch> action, CancellationToken cancellationToken = default) where TClassSearch : class, IClassSearch<T>, new()
        {
            var filter = new TClassSearch();
            action.Invoke(filter);

            var query = filter.ToMongoFilter();

            var collection = connection.Collection;

            var results = await collection.FindAsync(query, null, cancellationToken);
            var items = await results.ToListAsync();

            List<T> result = [];

            foreach (var item in items)
            {
                if (item.Document == null) continue;
                var key = keyManager.SetKey(item);
                cache.Add(key, item, cacheOptions);
                result.Add(item.Document);
                if (item.Document is IDocumentFileInternal internalDocument)
                {
                    internalDocument.TrackChanges();
                }
            }

            return result;
        }

        public async Task<IEnumerable<T>> MetadataSearch<TMetaSearch>(Action<TMetaSearch> action, CancellationToken cancellationToken = default)
            where TMetaSearch : class, IMetadataSearchBase, new()
        {
            var queryMeta = new TMetaSearch();
            action?.Invoke(queryMeta);

            var query = queryMeta.ToMongoFilter<T>();

            var collection = connection.Collection;

            var cursor = await collection.Find(query).ToListAsync(cancellationToken);

            List<T> results = [];

            foreach (var result in cursor)
            {
                if (result.Document == null) continue;
                var key = keyManager.SetKey(result);
                cache.Add(key, result, cacheOptions);
                results.Add(result.Document);
                if (result.Document is IDocumentFileInternal internalDocument)
                {
                    internalDocument.TrackChanges();
                }
            }

            return results;
        }

        public async Task<IEnumerable<T>> CombinedSearch<TClassSearch, TMetaSearch>(Action<TClassSearch>? queryAction, Action<TMetaSearch>? metaAction, SortDefinition<MongoDocument<T>> sort, int limit = 0, int skip = 0, CancellationToken cancellationToken = default)
            where TClassSearch : class, IClassSearch<T>, new()
            where TMetaSearch : class, IMetadataSearchBase, new()
        {
            var combinedFilter = BuildCombinedFilter(queryAction, metaAction);

            var collection = connection.Collection;
            var results = await collection.Find(combinedFilter)
                .Sort(sort)
                .Limit(limit)
                .Skip(skip)
                .As<BsonDocument>()
                .ToListAsync(cancellationToken);

            List<T> result = [];

            foreach (var item in results)
            {
                long version = 0;
                if (item.TryGetValue("Metadata", out var metadataNode) &&
                    metadataNode.AsBsonDocument.TryGetValue("Version", out var versionNode))
                {
                    version = versionNode.AsInt64;
                }

                var docId = item["_id"].ToString() ?? throw new InvalidOperationException("Document ID is null");
                cache.Add<T>(docId, item, cacheOptions);

                // for now throw an error, future log and continaue
                var typedDocument = BsonSerializer.Deserialize<MongoDocument<T>>(item) ?? throw new Exception("Invalid item type returned");
                typedDocument.Document?.Version = version;

                keyManager.SetKey(typedDocument);
                result.Add(typedDocument.Document!);

                if (isTrackable)
                {
                    ((IDocumentFileInternal)typedDocument.Document!).TrackChanges();
                }
            }

            return result;
        }

        public async Task<IEnumerable<TProjection>> SearchWithProjection<TClassSearch, TMetaSearch, TProjection>(
            Action<TClassSearch>? queryAction, Action<TMetaSearch>? metaAction, TProjection projection, SortDefinition<MongoDocument<T>> sort, int limit = 0, int skip = 0, CancellationToken cancellationToken = default)
            where TClassSearch : class, IClassSearch<T>, new()
            where TMetaSearch : class, IMetadataSearchBase, new()
            where TProjection : class, IProjectionBase<T, TProjection>, new()
        {
            var combinedFilter = BuildCombinedFilter(queryAction, metaAction);
            var projectionDefinition = projection.ToMongoProjection();

            var results = await connection.Collection.Find(combinedFilter)
                .Sort(sort)
                .Project(projectionDefinition)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync(cancellationToken);

            // Cast results to TProjection - the projection expression creates TProjection instances
            return results.Cast<TProjection>();
        }

        public async Task<IEnumerable<TProjection>> SearchWithVector<TClassSearch, TMetaSearch, TProjection>(
            Action<TClassSearch>? queryAction, Action<TMetaSearch>? metaAction, TProjection projection, string index, string embeddingName, float[] embedding, int limit, int skip, int returnCount, int conciderFrom, CancellationToken cancellationToken = default)
            where TClassSearch : class, IClassSearch<T>, new()
            where TMetaSearch : class, IMetadataSearchBase, new()
            where TProjection : class, IProjectionBase<T, TProjection>, new()
        {
            if (!capabilities.SupportsVectorSearch) throw new InvalidOperationException("MongoDB Server does not support Vector Search");

            var combinedFilter = BuildCombinedFilter(queryAction, metaAction);
            var projectionDefinition = projection.ToMongoProjection();

            var options = new VectorSearchOptions<MongoDocument<T>>()
            {
                IndexName = index,
                Filter = combinedFilter,
                NumberOfCandidates = conciderFrom
            };

            var pipeline = new EmptyPipelineDefinition<MongoDocument<T>>()
                .VectorSearch(embeddingName, embedding, returnCount, options)
                .Project(projectionDefinition);

            var results = await connection.Collection.Aggregate(pipeline).ToListAsync(cancellationToken);

            // Cast results to TProjection - the projection expression creates TProjection instances
            return results.Cast<TProjection>();
        }

        public async Task<IEnumerable<TProjection>> SearchWithAutoVector<TClassSearch, TMetaSearch, TProjection, TField>(
            Action<TClassSearch>? queryAction, 
            Action<TMetaSearch>? metaAction, 
            TProjection projection, 
            string index, 
            Expression<Func<MongoDocument<T>, TField>> embeddingName, 
            string embedding, 
            int limit, 
            int skip, 
            int returnCount, 
            int conciderFrom,
            CancellationToken cancellationToken = default)
            where TClassSearch : class, IClassSearch<T>, new()
            where TMetaSearch : class, IMetadataSearchBase, new()
            where TProjection : class, IProjectionBase<T, TProjection>, new()
        {
            if (!capabilities.SupportsVectorSearch) throw new InvalidOperationException("MongoDB Server does not support AuthEmbed Vector Search");

            var combinedFilter = BuildCombinedFilter(queryAction, metaAction);
            var projectionDefinition = projection.ToMongoProjection();

            var options = new VectorSearchOptions<MongoDocument<T>>()
            {
                IndexName = index,
                Filter = combinedFilter,
                NumberOfCandidates = conciderFrom
            };

            var pipeline = new EmptyPipelineDefinition<MongoDocument<T>>()
                .VectorSearch(embeddingName, embedding, returnCount, options)
                .Project(projectionDefinition);

            var results = await connection.Collection.Aggregate(pipeline).ToListAsync(cancellationToken);

            // Cast results to TProjection - the projection expression creates TProjection instances
            return results.Cast<TProjection>();
        }

        //private void ProcessFiltersAndProjection<TClassSearch, TMetaSearch, TProjection>(Action<TClassSearch>? queryAction, Action<TMetaSearch>? metaAction, TProjection projection, out FilterDefinition<MongoDocument<T>> combinedFilter, out IMongoCollection<MongoDocument<T>> collection, out ProjectionDefinition<MongoDocument<T>, TProjection> projectionDefinition)
        //    where TClassSearch : class, IClassSearch<T>, new()
        //    where TMetaSearch : class, IMetadataSearchBase, new()
        //    where TProjection : class, IProjectionBase<T, TProjection>, new()
        //{
        //    var builder = Builders<MongoDocument<T>>.Filter;
        //    var filters = new List<FilterDefinition<MongoDocument<T>>>();

        //    if (queryAction != null)
        //    {
        //        var queryFilter = new TClassSearch();
        //        queryAction.Invoke(queryFilter);
        //        filters.Add(queryFilter.ToMongoFilter());
        //    }

        //    if (metaAction != null)
        //    {
        //        var metaFilter = new TMetaSearch();
        //        metaAction.Invoke(metaFilter);
        //        filters.Add(metaFilter.ToMongoFilter<T>());
        //    }

        //    combinedFilter = filters.Count == 0 ? builder.Empty : builder.And(filters);
        //    collection = connection.Collection;

        //    // Create projection instance to get the projection definition
        //    projectionDefinition = projection.ToMongoProjection();
        //}

        public async Task<long> DeleteMany<TClassSearch, TMetaSearch>(Action<TClassSearch>? queryAction, Action<TMetaSearch>? metaAction, CancellationToken cancellationToken = default)
            where TClassSearch : class, IClassSearch<T>, new()
            where TMetaSearch : class, IMetadataSearchBase, new()
        {

            var combinedFilter = BuildCombinedFilter(queryAction, metaAction);

            var collection = connection.Collection;
            var result = await connection.Collection.DeleteManyAsync(combinedFilter, cancellationToken);

            return result.DeletedCount;
        }

        public async Task<T?> GetDocument(string key, CancellationToken cancellationToken = default)
        {
            // firat see if document is in cache
            if (cache.TryGet<T>(key, out MongoDocument<T>? doc))
            {
                if (doc != null && doc.Document != null)
                {

                    // we need to set it the key here as well, because the memory cache
                    // can hold references between scopes
                    keyManager.SetKey(doc);
                    return doc.Document;
                }
            }

            var collection = connection.Collection;

            var keyFilter = Builders<MongoDocument<T>>.Filter.Eq("_id", key);
            var result = await collection.FindAsync<BsonDocument>(keyFilter, null, cancellationToken);
            var mongoDoc = await result.FirstOrDefaultAsync();

            if (mongoDoc != null && mongoDoc.Contains("Document"))
            {
                long version = 0;
                if (mongoDoc.TryGetValue("Metadata", out var metadataNode) &&
                    metadataNode.AsBsonDocument.TryGetValue("Version", out var versionNode))
                {
                    version = versionNode.AsInt64;
                }

                var document = BsonSerializer.Deserialize<MongoDocument<T>>(mongoDoc) ?? throw new Exception("Invalid item type returned");

                document.Document!.Version = version;

                if (isTrackable)
                {
                    ((IDocumentFileInternal)document.Document!).TrackChanges();
                }
                keyManager.SetKey(document);
                cache.Add<T>(document.Id, mongoDoc, cacheOptions);
                return document.Document;
            }

            return null;
        }

        public async Task<DeleteResult> DeleteDocument(T document, CancellationToken cancellationToken = default)
        {
            var key = GetKey(document) ?? throw new InvalidOperationException("Document is not being tracked. Cannot delete.");

            var keyFilter = Builders<MongoDocument<T>>.Filter.Eq("_id", key);
            var collection = connection.Collection;

            var result = await collection.DeleteOneAsync(keyFilter, cancellationToken);

            if (result.DeletedCount > 0)
            {
                cache.Remove<T>(key);
            }
            return result;
        }

        public async Task<SaveChangesResult> UpdateDocument(T document, IMongoLockScope? lockKey = null, CancellationToken cancellationToken = default)
        {
            var key = GetKey(document);

            if (key == null)
            {
                throw new InvalidOperationException("Document is not being tracked. Cannot update.");
            }

            var keyFilter = Builders<MongoDocument<T>>.Filter
                .And(
                    Builders<MongoDocument<T>>.Filter.Eq("_id", key),
                    Builders<MongoDocument<T>>.Filter.Eq("Metadata.Version", new BsonInt64(document.Version))
                    );
            var value = await CapabilityCheck(document, lockKey, keyFilter);
            if (value is not null)
            {
                return value;
            }

            throw new InvalidOperationException("Invalid IDocumentFile submitted");
        }

        private async Task<SaveChangesResult?> CapabilityCheck(T document, IMongoLockScope? lockKey, FilterDefinition<MongoDocument<T>> keyFilter, CancellationToken cancellationToken = default)
        {
            switch (capabilities.ClusterType)
            {
                case ClusterType.ReplicaSet:
                case ClusterType.LoadBalanced:
                case ClusterType.Sharded:
                    SaveChangesResult? value = await UpdateWithReplica(document, lockKey, keyFilter, !IsCollectionEncrypted(connection.Collection), cancellationToken);
                    if (value != null)
                    {
                        return value;
                    }
                    break;
                case ClusterType.Unknown:
                case ClusterType.Standalone:
                    SaveChangesResult? value2 = await UpdateWithOutReplica(document, lockKey, keyFilter, !IsCollectionEncrypted(connection.Collection), cancellationToken);
                    if (value2 != null)
                    {
                        return value2;
                    }
                    break;
                default: throw new InvalidOperationException("Unknown ClusterType please report for a fix");
            }

            return null;
        }

        public async Task<SaveChangesResult> UpdateDocument<TMetaSearch>(T document, Action<TMetaSearch> metadata, IMongoLockScope? lockKey = null)
            where TMetaSearch : class, IMetadataSearchBase, new()
        {
            var queryMeta = new TMetaSearch();
            metadata.Invoke(queryMeta);

            // this should satisify OCC
            queryMeta.Version = document.Version;
            var filter = queryMeta.ToMongoFilter<T>();

            var value = await CapabilityCheck(document, lockKey, filter);
            if (value is not null)
                return value;

            throw new InvalidOperationException("Invalid metadata or Document was supplied for Update");
        }

        public string? GetKey(T document)
        {
            keyManager.TryGetKey(document, out var key);

            return key;
        }

        public bool IsCollectionEncrypted(IMongoCollection<MongoDocument<T>> collection)
        {
            if (_encrypted is not null)
            {
                return _encrypted.Value;
            }
            
            var settings = collection.Database.Client.Settings;
            _encrypted = settings.AutoEncryptionOptions != null;
            return _encrypted.Value;
        }

        private bool TryGetPendingUpdates(IDocumentFileInternal document, bool withPipeline,
            out UpdateDefinition<MongoDocument<T>>? updates) =>
            withPipeline
                ? document.TryGetPendingUpdatesPipeline<T>(out updates)
                : document.TryGetPendingUpdates<T>(out updates);

        private async Task<SaveChangesResult> ExecuteCoreUpdate(
            IDocumentFileInternal internalDocument,
            T document,
            IMongoLockScope? lockKey,
            FilterDefinition<MongoDocument<T>> keyFilter,
            UpdateDefinition<MongoDocument<T>> updates,
            IClientSessionHandle? session = null,
            CancellationToken cancellationToken = default)
        {
            if (await lockManager.IsLockedByOther(lockKey, document))
                return SaveChangesResult.Failed("Document is locked");

            var changed = session != null
                ? await connection.Collection.UpdateOneAsync(session, keyFilter, updates, null, cancellationToken)
                : await connection.Collection.UpdateOneAsync(keyFilter, updates, null, cancellationToken);

            internalDocument.ClearChanges();
            return SaveChangesResult.Success(changed);
        }

        private async Task<SaveChangesResult?> UpdateWithReplica(
            T document, IMongoLockScope? lockKey,
            FilterDefinition<MongoDocument<T>> keyFilter, bool withPipeline, CancellationToken cancellationToken = default)
        {
            if (document is not IDocumentFileInternal internalDocument)
                return null;

            if (!TryGetPendingUpdates(internalDocument, withPipeline, out var updates))
                return SaveChangesResult.Failed("Cannot update when no changes Found");

            using var session = await client.StartSessionAsync(null, cancellationToken);
            session.StartTransaction();
            try
            {
                var result = await ExecuteCoreUpdate(internalDocument, document, lockKey, keyFilter, updates!, session);

                if (result.SuccessResult)
                    await session.CommitTransactionAsync(cancellationToken);
                else
                    await session.AbortTransactionAsync(cancellationToken);

                return result;
            }
            catch (Exception)
            {
                await session.AbortTransactionAsync();
                return SaveChangesResult.Failed("Saves changes failed with a mongo Error");
            }
        }

        private async Task<SaveChangesResult?> UpdateWithOutReplica(
            T document, IMongoLockScope? lockKey,
            FilterDefinition<MongoDocument<T>> keyFilter, bool withPipeline, CancellationToken cancellationToken = default)
        {
            if (document is not IDocumentFileInternal internalDocument)
                return null;

            if (!TryGetPendingUpdates(internalDocument, withPipeline, out var updates))
                return SaveChangesResult.Failed("Cannot update when no changes Found");

            try
            {
                return await ExecuteCoreUpdate(internalDocument, document, lockKey, keyFilter, updates!);
            }
            catch (Exception)
            {
                return SaveChangesResult.Failed("Saves changes failed with a mongo Error");
            }
        }

        private FilterDefinition<MongoDocument<T>> BuildCombinedFilter<TClassSearch, TMetaSearch>(
            Action<TClassSearch>? queryAction, Action<TMetaSearch>? metaAction)
            where TClassSearch : class, IClassSearch<T>, new()
            where TMetaSearch : class, IMetadataSearchBase, new()
        {
            var builder = Builders<MongoDocument<T>>.Filter;
            var filters = new List<FilterDefinition<MongoDocument<T>>>();

            if (queryAction != null)
            {
                var queryFilter = new TClassSearch();
                queryAction.Invoke(queryFilter);
                filters.Add(queryFilter.ToMongoFilter());
            }

            if (metaAction != null)
            {
                var metaFilter = new TMetaSearch();
                metaAction.Invoke(metaFilter);
                filters.Add(metaFilter.ToMongoFilter<T>());
            }

            return filters.Count == 0 ? builder.Empty : builder.And(filters);
        }
    }
}