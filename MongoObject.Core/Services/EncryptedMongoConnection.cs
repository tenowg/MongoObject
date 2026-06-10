using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;
using MongoOptions.Services;

namespace MongoObject.Core.Services
{
    internal class EncryptedMongoConnection<T> : MongoConnection<T> where T : class, IDocumentFile, new()
    {
        public EncryptedMongoConnection([FromKeyedServices("SecuredClient")] IMongoClient client, MongoObjectOptions options, IDocumentTokenChangeMonitor<T> changeMonitor, InternalCacheService cache, IDocumentKeyManager keyManager) : base(client, options, changeMonitor, cache, keyManager)
        {
        }
    }
}
