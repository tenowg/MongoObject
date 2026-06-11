using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;

namespace MongoOptions.Services
{
    public class InternalCacheService(IMemoryCache cache, MongoObjectOptions options)
    {
        public void Remove<T>(string key)
        {
            cache.Remove(BuildKey<T>(key));
        }

        public static void Clear() { }

        public void Add<T>(string key, MongoDocument<T> value, MemoryCacheEntryOptions options)
            where T : class, IDocumentFile, new()
        {
            var bson = value.ToBsonDocument();
            cache.Set(BuildKey<T>(key), bson, options);
        }

        public bool TryGet<T>(string key, out MongoDocument<T>? doc)
            where T : class, IDocumentFile, new()
        {
            if(cache.TryGetValue(BuildKey<T>(key), out BsonDocument? value))
            {
                doc = BsonSerializer.Deserialize<MongoDocument<T>>(value);
                if (doc.Document == null)
                {
                    // this is an error that we will need to log.
                    return false;
                }
                doc.Document.Version = doc.Metadata["Version"].AsInt64;
                return true;
            }
            doc = null;
            return false;
        }

        private string BuildKey<T>(string key)
        {
            return $"{options.CachePrefix}{typeof(T).Name}_{key}";
        }
    }
}