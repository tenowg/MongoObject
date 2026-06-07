using Microsoft.Extensions.Hosting;
using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;

namespace MongoObject.Core.Services
{
    internal class BuildIndexesHostService(IEnumerable<IIndexBuilder> builders, IServiceProvider sp, MongoObjectOptions options) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var builder in builders)
            {
                await builder.EnsureIndexExists(sp, options.IsAtlasMongoDBInstance);
            }
        }
    }
}
