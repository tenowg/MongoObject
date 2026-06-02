using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;
using MongoOptions.Services;

namespace MongoObject.Core.Services
{
    internal class MongoDocumentManager<T>(IMongoConnection<T> connection,
                                           IDocumentKeyManager keyManager,
                                           DistributedLockManager lockManager,
                                           InternalCacheService cache,
                                           IMongoClient client,
                                           MongoObjectOptions options) 
        where T : class, IDocumentFile, new()
    {
        private readonly MemoryCacheEntryOptions cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = options.CacheHardDuration,
            SlidingExpiration = options.CacheSoftDuration
        };

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
                //_entityTracker.AddOrUpdate(item.Document, item.Id);
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

        public async Task DeleteDocument(T document)
        {
        }

        public async Task<SaveChangesResult> UpdateDocument(T document, IMongoLockScope? lockKey = null)
        {
            var key = GetKey(document);

            if (key == null)
            {
                throw new InvalidOperationException("Document is not being tracked. Cannot update.");
            }

            var keyFilter = Builders<MongoDocument<T>>.Filter.Eq("_id", key);

            if (document is IDocumentFileInternal internalDocument)
            {
                if(!internalDocument.TryGetPendingUpdatesPipeline<T>(out var updates))
                {
                    return SaveChangesResult.Failed("Cannot update when no changes Found");
                }

                using var trans = await client.StartSessionAsync();
                
                trans.StartTransaction();
                try
                {
                    var lockData = await lockManager.IsLocked(lockKey, document);    

                    if (lockData)
                    {
                        trans.AbortTransaction();
                        return SaveChangesResult.Failed($"Document is locked");
                    }

                    await connection.Collection.UpdateOneAsync(keyFilter, updates);
                    internalDocument.ClearChanges();
                    await trans.CommitTransactionAsync();
                }
                catch (Exception ex)
                {
                    await trans.AbortTransactionAsync();
                    return SaveChangesResult.Failed($"Saves changes failed with a mongo Error");
                }
                return SaveChangesResult.Success;
            }

            throw new InvalidOperationException("Invalid IDocumentFile submitted");
        }

        public async Task<SaveChangesResult> UpdateDocument<TMetaSearch>(T document, Action<TMetaSearch> metadata, IMongoLockScope? lockKey = null)
            where TMetaSearch : class, IMetadataSearchBase, new()
        {
            //var lastUpdated = Builders<MongoDocument<T>>.Update.Set("Metadata.LastModifiedAt", DateTime.UtcNow);
            if (document is IDocumentFileInternal internalDocument)
            {
                if(!internalDocument.TryGetPendingUpdatesPipeline<T>(out var updates))
                {
                    return SaveChangesResult.Failed("Cannot update document when now changes are found");
                }

                using var trans = await client.StartSessionAsync();

                trans.StartTransaction();
                try
                {
                    var lockData = await lockManager.IsLocked(lockKey, document);

                    if (lockData)
                    {
                        trans.AbortTransaction();
                        return SaveChangesResult.Failed($"Document is locked");
                    }

                    //var finalUpdates = Builders<MongoDocument<T>>.Update.Combine(lastUpdated, updates);
                    var queryMeta = new TMetaSearch();
                    metadata.Invoke(queryMeta);
                    var filter = queryMeta.ToMongoFilter<T>();

                    var updateResult = await connection.Collection.UpdateOneAsync(filter, updates);
                    internalDocument.ClearChanges();
                    await trans.CommitTransactionAsync();
                }
                catch
                {
                    await trans.AbortTransactionAsync();
                    return SaveChangesResult.Failed($"Saves changes failed with a mongo Error");
                }
                return SaveChangesResult.Success;
            }

            throw new InvalidOperationException("Invalid metadata or Document was supplied for Update");
        }

        public string? GetKey(T document)
        {
            keyManager.TryGetKey(document, out var key);

            return key;
        }
    }
}
