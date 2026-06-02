using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        services.AddMongoObject(options =>
        {
            options.ConnectionString = "mongodb://localhost:27017/?directConnection=true";
            options.DatabaseName = "mydatabase";
        })
        .AddWatchStream()
        .RegisterDocumentsProgress();
        services.AddSingleton<App>();
    })
    .Build();

// Run the app
var app = host.Services.GetRequiredService<App>();
await app.Run();
