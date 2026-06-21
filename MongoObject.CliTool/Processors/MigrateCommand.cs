using CliTool;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.CliTool.Processors
{
    internal class MigrateCommand : Spectre.Console.Cli.Command<MigrateSettings>
    {
        protected override int Execute(CommandContext context, MigrateSettings settings, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
