using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;

namespace MongoObject.Core.Services
{
    internal class MongoDocumentWatcherPolling(IServiceProvider sp, ILogger<MongoDocumentWatcherPolling> logger, MongoObjectOptions options, IMongoClient client, IEnumerable<IMongoConnection> connections) : BackgroundService
    {
        private DateTime lastRun;
        private Dictionary<string, IMongoCollection<BsonDocument>> _collections;
        private Dictionary<string, IMongoConnection> _connections = [];
        private FilterDefinition<BsonDocument> _filterDefinition = Builders<BsonDocument>.Filter.Empty;
        private ProjectionDefinition<BsonDocument> _projectionDefinition = Builders<BsonDocument>.Projection.Include("_id");

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            lastRun = DateTime.UtcNow;

            using var timer = new PeriodicTimer(options.WatchPollInterval);
            _collections = connections.Select(o => new { Name = o.CollectionName, Collection = client.GetDatabase(o.DatabaseName).GetCollection<BsonDocument>(o.CollectionName) }).ToDictionary(o => o.Name, o => o.Collection);
            _connections = connections.ToDictionary(o => o.CollectionName, o => o);

            while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
            {
                _filterDefinition = Builders<BsonDocument>.Filter.Gt("Metadata.LastModifiedAt", lastRun);
                await PollDatabase();
                lastRun = DateTime.UtcNow;
            }
        }

        private async Task PollDatabase()
        {
            foreach (var collection in _collections) 
            {
                var result = await collection.Value.Find(_filterDefinition).Project(_projectionDefinition).ToListAsync();

                foreach(var item in result)
                { 
                    var id = item["_id"].AsString;
                    if (_connections.TryGetValue(collection.Key, out var connection))
                    {
                        // do onchange needs to be added to connection object
                        connection.OnChanged(id);
                    }
                }
            }

        }
    }
}
