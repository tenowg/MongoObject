using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;

namespace MongoObject.Core.Services
{
    internal class BuildIndexesHostService(IEnumerable<IIndexBuilder> builders, IEnumerable<IEncryptionBuilder> encBuilders, IServiceProvider sp, MongoObjectOptions options, IMongoClient client) : IHostedLifecycleService
    {
        private IMongoCollection<LockMetadata> _lockCollection = client.GetDatabase(options.MongoSystemDatabaseName).GetCollection<LockMetadata>(options.DistributedLockCollectionName);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await ExecuteAsync(cancellationToken);
        }

        public async Task StartedAsync(CancellationToken cancellationToken)
        {
        }

        public async Task StartingAsync(CancellationToken cancellationToken)
        {
            foreach (var builder in encBuilders)
            {
                await builder.BuildCollection();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }

        public async Task StoppedAsync(CancellationToken cancellationToken)
        {
        }

        public async Task StoppingAsync(CancellationToken cancellationToken)
        {
        }

        protected async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var builder in builders)
            {
                await builder.EnsureIndexExists(sp, options.IsAtlasMongoDBInstance);
            }

            var indexKeys = Builders<LockMetadata>.IndexKeys.Ascending(lockDoc => lockDoc.LockExpiresAt);
            var indexOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }; // Expire immediately after time passes
            _lockCollection.Indexes.CreateOne(new CreateIndexModel<LockMetadata>(indexKeys, indexOptions));
        }
    }
}
