using ConsoleSetup;
using ConsoleSetup.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using MongoObject.Core.Extensions;

namespace ConsoleSetup.Snippets
{
    internal class PolingSetup
    {
        public PolingSetup()
        {
            using IHost host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                // Make sure you register a IMonogClient
                services.AddSingleton<IMongoClient>(sp =>
                {
                    var mongoConnectionUrl = new MongoUrl("mongodb://localhost:27018/?directConnection=true");
                    var mongoClientSettings = MongoClientSettings.FromUrl(mongoConnectionUrl);

                    return new MongoClient(mongoClientSettings);
                });
                #region AddMongoPolling
                // This adds the MongoObject services to the dependency injection container.
                // You can configure the connection string and database name here, and you can also add other options if needed.
                // The AddWatchStream method is optional,
                // but it allows you to watch for changes in the database and automatically update your documents in memory.
                services.AddMongoObject(options =>
                {
                    options.DatabaseName = "mydatabase";
                })
                // The Polling MangoWatcher this is only for development and should never be used for production environments
                .AddWatchPolling()
                // This is the important part - this is where you register your documents.
                // You can have multiple calls to RegisterDocument, or you can have extension methods like this one to group them together.
                .RegisterDocumentsConsoleSetup();
                #endregion
                services.AddSingleton<App>();
            })
            .Build();
        }
    }
}
