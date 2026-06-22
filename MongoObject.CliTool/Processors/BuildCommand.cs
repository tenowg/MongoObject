using CliTool;
using MongoDB.Driver;
using Spectre.Console.Cli;
using MongoObject.CliTool.Helpers;
using MongoObject.CliTool.Data;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;

namespace MongoObject.CliTool.Processors
{
    internal class BuildCommand : Spectre.Console.Cli.Command<BuildSettings>
    {
        private IMongoClient? _client;
        private IMongoClient? _encryptedClient;
        private DocumentConfiguration? _documents = null;

        protected override int Execute(CommandContext context, BuildSettings settings, CancellationToken cancellationToken)
        {
            var projectPath = Path.GetFullPath(settings.Project);
            _documents = ResourceHelpers.BuildAndGatherResources(projectPath, settings.Environment);
            if (_documents == null)
            {
                Console.WriteLine("There was an error retrieving the Document Schema");
                return 1;
            }
            if (_documents.ConnectionString == null)
            {
                Console.WriteLine("Please be sure to provide a connection String in you package configuation.");
                return 1;
            }

    
            (_client, _encryptedClient) = ClientOperations.CreateClients(_documents);

            if (_documents.DocumentSchema == null)
            {
                Console.WriteLine("No Schema's where discovered.");
                return 1;
            }
            var databases = _documents.DocumentSchema.GroupBy(x => x.Value.DatabaseName);

            if (string.IsNullOrEmpty(_documents.DefaultDatabase))
            {
                Console.WriteLine("The default database is undefined, make sure MongoObject is configured Correctly and try again");
                return 1;
            }

            ClientOperations.GetDifferencesByObject(_client, _documents, out var databaseNames, out var existingCollections, out var newCollections);
            return 0;
        }
    }
}