using Microsoft.Extensions.DependencyInjection;
using MongoObject.Core.Interfaces;
using MongoObject.RedisDistributedLock.Services;

namespace MongoObject.Core.Extensions
{
    public static class RedisDistributedServicesExtensions
    {
        extension(MongoObjectBuilder builder)
        {
            /// <summary>
            /// Adds the Redis-backed distributed lock manager to MongoObject.
            /// <para>
            /// <b>Prerequisites:</b> You must register an <c>IConnectionMultiplexer</c> before calling this method.
            /// MongoObject.RedisDistributedLock does not register its own Redis client.
            /// </para>
            /// <example>
            /// <code>
            /// // Register your Redis client first
            /// services.AddSingleton&lt;IConnectionMultiplexer&gt;(
            ///     ConnectionMultiplexer.Connect("localhost:6379"));
            ///
            /// // Then add MongoObject with Redis locking
            /// services.AddMongoObject(options => { ... })
            ///         .AddRedisLockManager()
            ///         .RegisterDocumentsFromAssembly();
            /// </code>
            /// </example>
            /// </summary>
            public MongoObjectBuilder AddRedisLockManager()
            {
                builder.Services.AddSingleton<IDistributedLockManager, RedisDistributedLockManager>();
                return builder;
            }
        }
    }
}
