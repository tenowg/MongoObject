using System.ComponentModel;
using Spectre.Console.Cli;

namespace CliTool {
    internal class BuildSettings : CommandSettings
    {
        [CommandOption("-p|--project")]
        [Description("Overwrite existing files without prompting")]
        public string Project { get; init; } = ".";

        [CommandOption("-f|--framework")]
        [Description("The target framework to execute (required if the project multi-targets)")]
        public string? Framework { get; set; }

        [CommandOption("-e|--environment")]
        [Description("The target framework to execute (required if the project multi-targets)")]
        public string Environment { get; set; } = "Debug";
    }

    internal class MigrateSettings : CommandSettings
    {
        [CommandOption("-f|--force")]
        [Description("Overwrite existing files without prompting")]
        public bool Force { get; init; }
    }
}