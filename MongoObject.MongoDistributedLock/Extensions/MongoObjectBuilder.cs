using Microsoft.Extensions.DependencyInjection;
using MongoObject.Core.Interfaces;
using MongoObject.RedisDistributedLock.Services;

namespace MongoObject.Core.Extensions
{
    public static class DistributedLockExtensions
    {
        extension(MongoObjectBuilder builder)
        {
            public MongoObjectBuilder AddMongoLockManager()
            {
                builder.Services.AddSingleton<IDistributedLockManager, MongoDistributedLockManager>();
                return builder;
            }
        }
    }
}
