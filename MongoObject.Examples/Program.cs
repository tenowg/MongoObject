using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoObject.Core.Extensions;
using MongoObject.Examples;
using MongoObject.Examples.Extensions;

#region ConfigureMongoObject
using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        // This adds the MongoObject services to the dependency injection container.
        // You can configure the connection string and database name here, and you can also add other options if needed.
        // The AddWatchStream method is optional,
        // but it allows you to watch for changes in the database and automatically update your documents in memory.
        services.AddMongoObject(options =>
        {
            options.ConnectionString = "mongodb://localhost:27017/?directConnection=true";
            options.DatabaseName = "mydatabase";
        })
        .AddWatchStream()
        // This is the important part - this is where you register your documents.
        // You can have multiple calls to RegisterDocument, or you can have extension methods like this one to group them together.
        .RegisterDocumentsMongoObject_Examples();
        services.AddSingleton<App>();
    })
    .Build();
#endregion
// Run the app
var app = host.Services.GetRequiredService<App>();
await app.Run();