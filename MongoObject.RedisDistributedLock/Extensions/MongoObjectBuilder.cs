using Microsoft.Extensions.DependencyInjection;

namespace MongoObject.Core.Extensions
{
    public static class RedisDistributedServicesExtensions
    {
        extension(MongoObjectBuilder builder)
        {
            public MongoObjectBuilder AddRedisLockManager()
            {
                return builder;
            }
        }
    }
}
