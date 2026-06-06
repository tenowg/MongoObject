using Microsoft.Extensions.Hosting;
using MongoObject.Core.Interfaces;

namespace MongoObject.Core.Services
{
    internal class BuildIndexesHostService(IEnumerable<IIndexBuilder> builders, IServiceProvider sp) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var builder in builders)
            {
                await builder.EnsureIndexExists(sp);
            }
        }
    }
}
