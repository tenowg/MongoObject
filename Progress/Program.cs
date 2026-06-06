using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;
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



using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton<IMongoClient>(sp =>
        {
            var mongoConnectionUrl = new MongoUrl("mongodb://localhost:27018/?directConnection=true");
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

        services.AddMongoObject(options =>
        {
            options.DatabaseName = "mydatabase";
        })
        .AddWatchStream()
        .RegisterDocumentsProgress();
        services.AddSingleton<App>();
    })
    .Build();
await host.StartAsync();

// Run the app
var app = host.Services.GetRequiredService<App>();
await app.Run();

await host.WaitForShutdownAsync();
