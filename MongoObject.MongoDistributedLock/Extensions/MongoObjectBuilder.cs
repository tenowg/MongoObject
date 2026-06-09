using Microsoft.Extensions.DependencyInjection;

namespace MongoObject.MongoDistributedLock.Extensions
{
    public partial class MongoObjectBuilder(IServiceCollection sp)
    {
        public MongoObjectBuilder AddMongoLockManager()
        {
            return this;
        }
    }
}
