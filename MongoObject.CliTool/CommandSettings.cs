using System.ComponentModel;
using Spectre.Console.Cli;

namespace CliTool {
    internal abstract class GlobalSettings : CommandSettings
    {
        [CommandOption("-v|--verbose")]
        [Description("Display detailed logs for Debugging")]
        public bool Verbose { get; init; } = false;
        [CommandOption("-e|--environment")]
        [Description("The target environment to use")]
        public string Environment { get; set; } = "Debug";
        [CommandOption("-p|--project")]
        [Description("Overwrite existing files without prompting")]
        public string Project { get; init; } = ".";
    }

    internal class BuildSettings : GlobalSettings
    {
        [CommandOption("-f|--framework")]
        [Description("The target framework to execute (required if the project multi-targets)")]
        public string? Framework { get; set; }

        [CommandOption("-c|--ci-deploy")]
        [Description("This will launch a non-interactive version that validates the current migration against the live database")]
        public bool CIDeploy { get; set; }
    }

    internal class MigrateSettings : GlobalSettings
    {
    }
}