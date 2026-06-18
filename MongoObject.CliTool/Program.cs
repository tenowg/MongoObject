using CliTool;
using Spectre.Console.Cli;
using MongoObject.CliTool.Processors;

var app = new CommandApp<MigrateCommand>();
return app.Run(args);

internal class MigrateCommand : Command<Settings>
{
    private List<IProcessor> processors =
        [
            new MigrationProcessor()
        ];

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        foreach(var process in processors)
        {
            process.Execute(settings);
        }
        return 0;
    }
}