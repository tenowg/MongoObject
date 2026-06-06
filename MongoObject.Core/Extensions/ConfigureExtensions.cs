using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
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
            public MongoObjectBuilder AddMongoObject(Action<MongoObjectOptions> options)
            {
                var optionsInstance = new MongoObjectOptions();
                options(optionsInstance);
                services.AddSingleton(optionsInstance);
                services.AddSingleton<IDocumentKeyManager, MongoDocumentKeyManager>();
                services.AddMemoryCache();
                services.AddSingleton<DistributedLockManager>();
                services.AddSingleton<InternalCacheService>();
                services.AddHostedService<BuildIndexesHostService>();

                var objectSerializer = new ObjectSerializer(ObjectSerializer.AllAllowedTypes);
                BsonSerializer.RegisterSerializer(objectSerializer);

                return new MongoObjectBuilder(services);
            }
        }
    }
}
