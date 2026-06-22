using CliTool;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.CliTool.Processors
{
    internal class MigrateCommand : Spectre.Console.Cli.AsyncCommand<MigrateSettings>
    {
        protected override async Task<int> ExecuteAsync(CommandContext context, MigrateSettings settings, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
