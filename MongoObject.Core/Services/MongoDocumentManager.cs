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
        private readonly MemoryCacheEntryOptions cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = options.CacheHardDuration,
            SlidingExpiration = options.CacheSoftDuration
        };
        private bool isTrackable = typeof(IDocumentFileInternal).IsAssignableFrom(typeof(T));
        private string cacheKeyBase = cache.PrebuildKey<T>();

        public async Task<string> AddDocument<TMetaBase>(T document, Action<TMetaBase>? action)
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
            
            try
            {
                await collection.InsertOneAsync(mongoDocument);
            } catch
            {
                throw;
            }

            document.Version = meta.Version.Value;
            var key = keyManager.SetKey(mongoDocument);
            cache.Add(key, mongoDocument, cacheOptions);

            return key;
        }

        public async Task<IEnumerable<T>> ClassSearch<TClassSearch>(Action<TClassSearch> action) where TClassSearch : class, IClassSearch<T>, new()
        {
            var filter = new TClassSearch();
            action.Invoke(filter);

            var query = filter.ToMongoFilter();

            var collection = connection.Collection;

            var results = await collection.FindAsync(query);
            var items = await results.ToListAsync();

            List<T> result = new List<T>();

            foreach (var item in items)
            {
                if (item.Document == null) continue;
                var key = keyManager.SetKey(item);
                cache.Add(key, item, cacheOptions);
                result.Add(item.Document);
                if(item.Document is IDocumentFileInternal internalDocument)
                {
                    internalDocument.TrackChanges();
                }
            }

            return result;
        }

        public async Task<IEnumerable<T>> MetadataSearch<TMetaSearch>(Action<TMetaSearch> action)
            where TMetaSearch : class, IMetadataSearchBase, new()
        {
            var queryMeta = new TMetaSearch();
            action?.Invoke(queryMeta);

            var query = queryMeta.ToMongoFilter<T>();

            var collection = connection.Collection;

            var cursor = await collection.Find(query).ToListAsync();

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

        public async Task<IEnumerable<T>> CombinedSearch<TClassSearch, TMetaSearch>(Action<TClassSearch>? queryAction, Action<TMetaSearch>? metaAction, int limit = 0, int skip = 0)
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

            var combinedFilter = filters.Count == 0 ? builder.Empty : builder.And(filters);

            var collection = connection.Collection;
            var results = await collection.Find(combinedFilter)
                .Limit(limit)
                .Skip(skip)
                .As<BsonDocument>()
                .ToListAsync();

            List<T> result = [];

            foreach (var item in results)
            {
                long version = 0;
                if (item.TryGetValue("Metadata", out var metadataNode) &&
                    metadataNode.AsBsonDocument.TryGetValue("Version", out var versionNode))
                {
                    version = versionNode.AsInt64;
                }

                var docId = item["_id"].ToString();
                string cacheKey = cacheKeyBase + docId;

                cache.Add(cacheKey, item, cacheOptions);

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
            Action<TClassSearch>? queryAction, Action<TMetaSearch>? metaAction, TProjection projection, int limit = 0, int skip = 0)
            where TClassSearch : class, IClassSearch<T>, new()
            where TMetaSearch : class, IMetadataSearchBase, new()
            where TProjection : class, IProjectionBase<T, TProjection>, new()
        {
            ProcessFiltersAndProjection(queryAction, metaAction, projection, out FilterDefinition<MongoDocument<T>> combinedFilter, out IMongoCollection<MongoDocument<T>> collection, out ProjectionDefinition<MongoDocument<T>, TProjection> projectionDefinition);

            var results = await collection.Find(combinedFilter)
                .Project(projectionDefinition)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync();

            // Cast results to TProjection - the projection expression creates TProjection instances
            return results.Cast<TProjection>();
        }

        public async Task<IEnumerable<TProjection>> SearchWithVector<TClassSearch, TMetaSearch, TProjection>(
            Action<TClassSearch>? queryAction, Action<TMetaSearch>? metaAction, TProjection projection, string index, string embeddingName, float[] embedding, int limit, int skip, int returnCount, int conciderFrom)
            where TClassSearch : class, IClassSearch<T>, new()
            where TMetaSearch : class, IMetadataSearchBase, new()
            where TProjection : class, IProjectionBase<T, TProjection>, new()
        {
            if (!capabilities.SupportsVectorSearch) throw new InvalidOperationException("MongoDB Server does not support Vector Search");
            
            ProcessFiltersAndProjection(queryAction, metaAction, projection, out FilterDefinition<MongoDocument<T>> combinedFilter, out IMongoCollection<MongoDocument<T>> collection, out ProjectionDefinition<MongoDocument<T>, TProjection> projectionDefinition);

            var options = new VectorSearchOptions<MongoDocument<T>>()
            {
                IndexName = index,
                Filter = combinedFilter,
                NumberOfCandidates = conciderFrom
            };

            var pipeline = new EmptyPipelineDefinition<MongoDocument<T>>()
                .VectorSearch(embeddingName, embedding, returnCount, options)
                .Project(projectionDefinition);

            var results = await collection.Aggregate(pipeline).ToListAsync();

            // Cast results to TProjection - the projection expression creates TProjection instances
            return results.Cast<TProjection>();
        }

        public async Task<IEnumerable<TProjection>> SearchWithAutoVector<TClassSearch, TMetaSearch, TProjection, TField>(
            Action<TClassSearch>? queryAction, Action<TMetaSearch>? metaAction, TProjection projection, string index, Expression<Func<MongoDocument<T>, TField>> embeddingName, string embedding, int limit, int skip, int returnCount, int conciderFrom)
            where TClassSearch : class, IClassSearch<T>, new()
            where TMetaSearch : class, IMetadataSearchBase, new()
            where TProjection : class, IProjectionBase<T, TProjection>, new()
        {
            if (!capabilities.SupportsVectorSearch) throw new InvalidOperationException("MongoDB Server does not support AuthEmbed Vector Search");
            
            ProcessFiltersAndProjection(queryAction, metaAction, projection, out FilterDefinition<MongoDocument<T>> combinedFilter, out IMongoCollection<MongoDocument<T>> collection, out ProjectionDefinition<MongoDocument<T>, TProjection> projectionDefinition);

            var options = new VectorSearchOptions<MongoDocument<T>>()
            {
                IndexName = index,
                Filter = combinedFilter,
                NumberOfCandidates = conciderFrom
            };

            var pipeline = new EmptyPipelineDefinition<MongoDocument<T>>()
                .VectorSearch(embeddingName, embedding, returnCount, options)
                .Project(projectionDefinition);

            var results = await collection.Aggregate(pipeline).ToListAsync();

            // Cast results to TProjection - the projection expression creates TProjection instances
            return results.Cast<TProjection>();
        }

        private void ProcessFiltersAndProjection<TClassSearch, TMetaSearch, TProjection>(Action<TClassSearch>? queryAction, Action<TMetaSearch>? metaAction, TProjection projection, out FilterDefinition<MongoDocument<T>> combinedFilter, out IMongoCollection<MongoDocument<T>> collection, out ProjectionDefinition<MongoDocument<T>, TProjection> projectionDefinition)
            where TClassSearch : class, IClassSearch<T>, new()
            where TMetaSearch : class, IMetadataSearchBase, new()
            where TProjection : class, IProjectionBase<T, TProjection>, new()
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

            combinedFilter = filters.Count == 0 ? builder.Empty : builder.And(filters);
            collection = connection.Collection;

            // Create projection instance to get the projection definition
            projectionDefinition = projection.ToMongoProjection();
        }

        public async Task<long> DeleteMany<TClassSearch, TMetaSearch>(Action<TClassSearch>? queryAction, Action<TMetaSearch>? metaAction)
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

            var combinedFilter = filters.Count == 0 ? builder.Empty : builder.And(filters);

            var collection = connection.Collection;
            var result = await collection.DeleteManyAsync(combinedFilter);

            return result.DeletedCount;
        }

        public async Task<T?> GetDocument(string key)
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
            // the his purely base line code
            
            var keyFilter = Builders<MongoDocument<T>>.Filter.Eq("_id", key);
            var result = await collection.FindAsync<MongoDocument<T>>(keyFilter);

            var mongoDoc = await result.FirstOrDefaultAsync();

            if (mongoDoc != null && mongoDoc.Document != null)
            {
                keyManager.SetKey(mongoDoc);
            }

            return result.FirstOrDefault().Document;
        }

        public async Task<DeleteResult> DeleteDocument(T document)
        {
            var key = GetKey(document) ?? throw new InvalidOperationException("Document is not being tracked. Cannot delete.");

            var keyFilter = Builders<MongoDocument<T>>.Filter.Eq("_id", key);
            var collection = connection.Collection;

            var result = await collection.DeleteOneAsync(keyFilter);

            if (result.DeletedCount >0)
            {
                cache.Remove<T>(key);
            }
            return result;
        }

        public async Task<SaveChangesResult> UpdateDocument(T document, IMongoLockScope? lockKey = null)
        {
            var key = GetKey(document);

            if (key == null)
            {
                throw new InvalidOperationException("Document is not being tracked. Cannot update.");
            }

            var keyFilter = Builders<MongoDocument<T>>.Filter
                .And(
                    Builders<MongoDocument<T>>.Filter.Eq("_id", key),
                    Builders<MongoDocument<T>>.Filter.Eq("Metadata.Version", document.Version
                    ));

            switch (capabilities.ClusterType)
            {
                case ClusterType.ReplicaSet:
                case ClusterType.LoadBalanced:
                case ClusterType.Sharded:
                    (bool flowControl, SaveChangesResult? value) = await UpdateWithReplica(document, lockKey, keyFilter, !IsCollectionEncrypted(connection.Collection));
                    if (!flowControl && value != null)
                    {
                        return value;
                    }
                    break;
                case ClusterType.Unknown:
                case ClusterType.Standalone:
                    (bool flowControl2, SaveChangesResult? value2) = await UpdateWithOutReplica(document, lockKey, keyFilter, !IsCollectionEncrypted(connection.Collection));
                    if (!flowControl2 && value2 != null)
                    {
                        return value2;
                    }
                    break;
                default: throw new InvalidOperationException("Unknown ClusterType please report for a fix");
            }

            throw new InvalidOperationException("Invalid IDocumentFile submitted");
        }

        private async Task<(bool flowControl, SaveChangesResult? value)> UpdateWithReplica(T document, IMongoLockScope? lockKey, FilterDefinition<MongoDocument<T>> keyFilter, bool withPipeline)
        {
            if (document is IDocumentFileInternal internalDocument)
            {
                UpdateDefinition<MongoDocument<T>>? updates;
                if (withPipeline)
                {
                    if (!internalDocument.TryGetPendingUpdatesPipeline<T>(out updates))
                    {
                        return (flowControl: false, value: SaveChangesResult.Failed("Cannot update when no changes Found"));
                    }
                }
                else
                {
                    if (!internalDocument.TryGetPendingUpdates<T>(out updates))
                    {
                        return (flowControl: false, value: SaveChangesResult.Failed("Cannot update when no changes Found"));
                    }
                }

                using var trans = await client.StartSessionAsync();

                trans.StartTransaction();
                try
                {
                    var lockData = await lockManager.IsLockedByOther(lockKey, document);

                    if (lockData)
                    {

                        trans.AbortTransaction();
                        return (flowControl: false, value: SaveChangesResult.Failed($"Document is locked"));
                    }

                    await connection.Collection.UpdateOneAsync(keyFilter, updates);
                    internalDocument.ClearChanges();
                    await trans.CommitTransactionAsync();
                }
                catch (Exception)
                {
                    await trans.AbortTransactionAsync();
                    return (flowControl: false, value: SaveChangesResult.Failed($"Saves changes failed with a mongo Error"));
                }
                return (flowControl: false, value: SaveChangesResult.Success);
            }

            return (flowControl: true, value: null);
        }

        private async Task<(bool flowControl, SaveChangesResult? value)> UpdateWithOutReplica(T document, IMongoLockScope? lockKey, FilterDefinition<MongoDocument<T>> keyFilter, bool withPipeline)
        {
            if (document is IDocumentFileInternal internalDocument)
            {
                UpdateDefinition<MongoDocument<T>>? updates;
                if (withPipeline)
                {
                    if (!internalDocument.TryGetPendingUpdatesPipeline<T>(out updates))
                    {
                        return (flowControl: false, value: SaveChangesResult.Failed("Cannot update when no changes Found"));
                    }
                }
                else
                {
                    if (!internalDocument.TryGetPendingUpdates<T>(out updates))
                    {
                        return (flowControl: false, value: SaveChangesResult.Failed("Cannot update when no changes Found"));
                    }
                }

                try
                {
                    var lockData = await lockManager.IsLockedByOther(lockKey, document);

                    if (lockData)
                    {
                        return (flowControl: false, value: SaveChangesResult.Failed($"Document is locked"));
                    }

                    await connection.Collection.UpdateOneAsync(keyFilter, updates);
                    internalDocument.ClearChanges();
                }
                catch (Exception)
                {
                    return (flowControl: false, value: SaveChangesResult.Failed($"Saves changes failed with a mongo Error"));
                }
                return (flowControl: false, value: SaveChangesResult.Success);
            }

            return (flowControl: true, value: null);
        }

        public async Task<SaveChangesResult> UpdateDocument<TMetaSearch>(T document, Action<TMetaSearch> metadata, IMongoLockScope? lockKey = null)
            where TMetaSearch : class, IMetadataSearchBase, new()
        {
            var queryMeta = new TMetaSearch();
            metadata.Invoke(queryMeta);

            // this should satisify OCC
            queryMeta.Version = document.Version;
            var filter = queryMeta.ToMongoFilter<T>();

            switch (capabilities.ClusterType)
            {
                case ClusterType.ReplicaSet:
                case ClusterType.LoadBalanced:
                case ClusterType.Sharded:
                    (bool flowControl, SaveChangesResult? value) = await UpdateWithReplica(document, lockKey, filter, !IsCollectionEncrypted(connection.Collection));
                    if (!flowControl && value != null)
                    {
                        return value;
                    }
                    break;
                case ClusterType.Unknown:
                case ClusterType.Standalone:
                    (bool flowControl2, SaveChangesResult? value2) = await UpdateWithOutReplica(document, lockKey, filter, !IsCollectionEncrypted(connection.Collection));
                    if (!flowControl2 && value2 != null)
                    {
                        return value2;
                    }
                    break;
                default: throw new InvalidOperationException("Unknown ClusterType please report for a fix");
            }

            throw new InvalidOperationException("Invalid metadata or Document was supplied for Update");
        }

        public string? GetKey(T document)
        {
            keyManager.TryGetKey(document, out var key);

            return key;
        }

        public static bool IsCollectionEncrypted(IMongoCollection<MongoDocument<T>> collection)
        {
            var settings = collection.Database.Client.Settings;
            return settings.AutoEncryptionOptions != null;
        }
    }
}
