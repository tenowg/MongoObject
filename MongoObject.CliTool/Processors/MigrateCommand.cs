using CliTool;
using MongoObject.CliTool.Helpers;
using Spectre.Console.Cli;

namespace MongoObject.CliTool.Processors
{
    internal class MigrateCommand : Spectre.Console.Cli.AsyncCommand<MigrateSettings>
    {
        protected override async Task<int> ExecuteAsync(CommandContext context, MigrateSettings settings, CancellationToken cancellationToken)
        {
            var projectPath = Path.GetFullPath(settings.Project);
            return await ResourceHelpers.BuildAndExecuteMigrations(projectPath, settings.Environment, settings.Verbose, cancellationToken);
        }
    }
}
