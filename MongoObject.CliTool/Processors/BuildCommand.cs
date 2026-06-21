using CliTool;
using MongoDB.Driver.Encryption;
using MongoObject.CliTool.Data;
using MongoDB.Driver;
using MongoObject.Core.Data;
using Spectre.Console.Cli;
using MongoObject.CliTool.Helpers;

namespace MongoObject.CliTool.Processors
{
    internal class BuildCommand : Spectre.Console.Cli.Command<BuildSettings>
    {
        private IMongoClient? _client;
        private IMongoClient? _encryptedClient;

        protected override int Execute(CommandContext context, BuildSettings settings, CancellationToken cancellationToken)
        {
            var projectPath = Path.GetFullPath(settings.Project);
            var documents = ResourceHelpers.BuildAndGatherResources(projectPath, settings.Environment);
            if (documents == null)
            {
                Console.WriteLine("There was an error retrieving the Document Schema");
                return 1;
            }
            if (documents.ConnectionString == null)
            {
                Console.WriteLine("Please be sure to provide a connection String in you package configuation.");
                return 1;
            }
            (_client, _encryptedClient) = ResourceHelpers.CreateClients(documents);

            return 0;
        }
    }
}