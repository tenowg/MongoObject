using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.RedisDistributedLock.Extensions
{
    public partial class MongoObjectBuilder(IServiceCollection sp)
    {
        public MongoObjectBuilder AddRedisLockManager()
        {
            return this;
        }
    }
}
