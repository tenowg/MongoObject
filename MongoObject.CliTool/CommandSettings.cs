using System.ComponentModel;
using Spectre.Console.Cli;

namespace CliTool {

    public enum MigrationOptions
    {
        None,
        Migrate
    }

    internal class Settings : CommandSettings
    {
        [CommandArgument(0, "<action>")]
        [Description("MongoObject Migration builder and runner")]
        public MigrationOptions Action { get; init; } = MigrationOptions.None;

        [CommandOption("-b|--build")]
        [Description("Overwrite existing files without prompting")]
        public bool Build { get; init; }

        [CommandOption("-m|--migrate")]
        [Description("Overwrite existing files without prompting")]
        public bool Migrate { get; init; }

        [CommandOption("-p|--project")]
        [Description("Overwrite existing files without prompting")]
        public string Project { get; init; } = ".";
    }
}