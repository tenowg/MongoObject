using Spectre.Console.Cli;
using MongoObject.CliTool.Processors;

var cancellationTokenSource = new CancellationTokenSource();
  
System.Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellationTokenSource.Cancel();
    System.Console.WriteLine("Cancellation requested...");
};

var app = new CommandApp();
app.Configure(config =>
{
    config.AddBranch("migrate", add =>
    {
        add.SetDescription("Build and Run Migrations for MongoObjects");
        add.AddCommand<BuildCommand>("build")
            .WithDescription("Build the migration operation class, this is your first step"); ;
        add.AddCommand<MigrateCommand>("run")
            .WithDescription("Run the migration operation class, this is your second step");
    });
});
return await app.RunAsync(args, cancellationTokenSource.Token);