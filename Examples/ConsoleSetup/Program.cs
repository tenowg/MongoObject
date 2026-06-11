using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using MongoObject.Core.Extensions;
using ConsoleSetup.Extensions;
using ConsoleSetup;

#region ConfigureMongoObject
using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        // Make sure you register a IMonogClient
        services.AddSingleton<IMongoClient>(sp =>
        {
            var mongoConnectionUrl = new MongoUrl("mongodb://localhost:27018/?directConnection=true");
            var mongoClientSettings = MongoClientSettings.FromUrl(mongoConnectionUrl);

            return new MongoClient(mongoClientSettings);
        });

        // This adds the MongoObject services to the dependency injection container.
        // You can configure the database name here, and you can also add other options if needed.
        // The AddWatchStream method is optional,
        // but it allows you to watch for changes in the database and automatically update your documents in memory.
        services.AddMongoObject((builder, options) =>
        {
            options.DatabaseName = "mydatabase";
            builder.AddWatchStream()
            // This is the important part - this is where you register your documents.
            // You can have multiple calls to RegisterDocument, or you can use the provided extension method from source generation (highly recommended).
            .RegisterDocumentsConsoleSetup();
        });
        services.AddSingleton<App>();
    })
    .Build();

await host.StartAsync();
#endregion
// Run the app
var app = host.Services.GetRequiredService<App>();
await app.Run();