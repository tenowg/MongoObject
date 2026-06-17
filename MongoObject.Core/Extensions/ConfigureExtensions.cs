using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Bson;
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
            public IServiceCollection AddMongoObject(IConfiguration config, Action<MongoObjectBuilder, MongoObjectOptions> configure)
            {
                var args = Environment.GetCommandLineArgs();
                
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
                    hook(services, config);
                }

                services.TryAddSingleton<IDistributedLockManager, NoOpLockManager>();

                if (args.Contains("--mongoobject-dump-schema"))
                {
                    var bson = MongoObjectsPluginRegistry.SchemaDocument;
                    Console.WriteLine($"cli-data: {bson}");
                    Environment.Exit(0);
                }
                return services;
            }
        }
    }
}
