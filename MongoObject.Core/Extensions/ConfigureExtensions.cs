using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;
using MongoObject.Core.Services;
using MongoOptions.Services;

namespace MongoObject.Core.Extensions
{
    public static class ConfigureExtensions
    {
        extension(IServiceCollection services)
        {
            public IServiceCollection AddMongoObject(Action<MongoObjectBuilder, MongoObjectOptions> configure)
            {
                var optionsInstance = new MongoObjectOptions();
                var mongoBuilder = new MongoObjectBuilder(services);
                configure(mongoBuilder, optionsInstance);
                services.AddSingleton(optionsInstance);

                services.AddSingleton<IDocumentKeyManager, MongoDocumentKeyManager>();
                services.AddMemoryCache();
                services.AddSingleton<InternalCacheService>();
                services.AddHostedService<BuildIndexesHostService>();
                services.AddSingleton<MongoServerCapabilities>(sp =>
                {
                    var client = sp.GetRequiredService<IMongoClient>();
                    return MongoServerCapabilities.Resolve(client);
                });

                var objectSerializer = new ObjectSerializer(ObjectSerializer.AllAllowedTypes);
                BsonSerializer.RegisterSerializer(objectSerializer);

                // lets add the Hooks
                foreach(var hook in MongoObjectsPluginRegistry.RegisterDocumentsHook)
                {
                    hook(services);
                }

                services.TryAddSingleton<IDistributedLockManager, NoOpLockManager>();

                return services;
            }
        }
    }
}
