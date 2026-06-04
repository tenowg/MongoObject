using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoObject.Core.Attributes;
using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;
using MongoOptions.Services;
using System.Reflection;

namespace MongoObject.Core.Services
{
    internal class MongoConnection<T> : IMongoCollection, IMongoConnection<T>
        where T : class, IDocumentFile, new()
    {
        public IMongoCollection<MongoDocument<T>> Collection { get; }
        public string CollectionName { get; }
        public string DatabaseName { get; }
        private IDocumentTokenChangeMonitor<T> _changeMonitor;
        private InternalCacheService _cache;
        private IDocumentKeyManager _keyManager;

        public MongoConnection(IMongoClient client, MongoObjectOptions options, IDocumentTokenChangeMonitor<T> changeMonitor, InternalCacheService cache, IDocumentKeyManager keyManager)
        {
            _changeMonitor = changeMonitor;
            _cache = cache;
            _keyManager = keyManager;
            var optionsAttr = typeof(T).GetCustomAttribute<MongoObjectAttribute>();
            CollectionName = optionsAttr?.CollectionName ?? typeof(T).Name;
            DatabaseName = optionsAttr?.DatabaseName ?? options.DatabaseName;

            Collection = client.GetDatabase(DatabaseName).GetCollection<MongoDocument<T>>(CollectionName);
        }

        public void Dispose()
        {
            
        }

        public void OnChanged(BsonDocument document)
        {
            var doc = BsonSerializer.Deserialize<MongoDocument<T>>(document);
            if (doc != null)
            {
                _cache.Remove<T>(doc.Id!);
                _changeMonitor.SignalChange(doc.Id!);
            }
        }
    }
}
