using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;
using MongoObject.Core.Services;
using MongoObject.PropertyEncryption.Interfaces;
using MongoOptions.Services;

namespace MongoObject.PropertyEncryption.Services
{
    public class EncryptedMongoConnection<T>(IEncryptedClient client,
                                               MongoObjectOptions options,
                                               IDocumentTokenChangeMonitor<T> changeMonitor,
                                               InternalCacheService cache,
                                               IDocumentKeyManager keyManager) : MongoConnection<T>(client, options, changeMonitor, cache, keyManager)
        where T : class, IDocumentFile, new()
    {
    }
}
