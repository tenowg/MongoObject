using System.ComponentModel;
using Spectre.Console.Cli;

namespace CliTool {
    internal abstract class GlobalSettings : CommandSettings
    {
        [CommandOption("-v|--verbose")]
        [Description("Display detailed logs for Debugging")]
        public bool Verbose { get; init; } = false;
    }

    internal class BuildSettings : GlobalSettings
    {
        [CommandOption("-p|--project")]
        [Description("Overwrite existing files without prompting")]
        public string Project { get; init; } = ".";

        [CommandOption("-f|--framework")]
        [Description("The target framework to execute (required if the project multi-targets)")]
        public string? Framework { get; set; }

        [CommandOption("-e|--environment")]
        [Description("The target environment to use")]
        public string Environment { get; set; } = "Debug";
    }

    internal class MigrateSettings : GlobalSettings
    {
        [CommandOption("-f|--force")]
        [Description("Overwrite existing files without prompting")]
        public bool Force { get; init; }
    }
}