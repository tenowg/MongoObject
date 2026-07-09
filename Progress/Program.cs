using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoObject.Core.Extensions;
using Progress;
using Progress.Extensions;

AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
{
    Console.WriteLine("Process is exiting!");
    Console.WriteLine(Environment.StackTrace);
};

AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    Console.WriteLine($"Unhandled exception: {e.ExceptionObject}");
};

IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<Program>() // This pulls keys from secrets.json
            .Build();

string apiKey = config["Mongo:Connection"];

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(async (_, services) =>
    {
        services.AddSingleton<IMongoClient>(sp =>
        {
            var mongoConnectionUrl = new MongoUrl(apiKey);
            var mongoClientSettings = MongoClientSettings.FromUrl(mongoConnectionUrl);

            // Log everything to the console
            mongoClientSettings.ClusterConfigurator = cb =>
            {
                cb.Subscribe<CommandStartedEvent>(e =>
                {
                    Console.WriteLine($"{e.CommandName} - {e.Command.ToJson()}");
                });
            };

            return new MongoClient(mongoClientSettings);
        });

        await services.AddMongoObject(config, (builder, options) =>
        {
            options.ConnectionString = apiKey;
            options.DatabaseName = "mydatabase";
            options.IsAtlasMongoDBInstance = true;

            builder.AddWatchStream()
                .AddMongoLockManager()
                .AddMongoEncryption(options =>
                {
                    options.ConnectionString = apiKey;
                })
            .RegisterDocumentsProgress();
        });

        services.AddSingleton<App>();
    })
    .Build();
await host.StartAsync();

// Run the app
var app = host.Services.GetRequiredService<App>();
await app.Run();

await host.WaitForShutdownAsync();
