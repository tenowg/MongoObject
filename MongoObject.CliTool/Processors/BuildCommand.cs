using CliTool;
using MongoDB.Driver;
using Spectre.Console.Cli;
using MongoObject.CliTool.Helpers;
using MongoObject.CliTool.Data;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
using Spectre.Console;

namespace MongoObject.CliTool.Processors
{
    internal class BuildCommand : Spectre.Console.Cli.AsyncCommand<BuildSettings>
    {
        private IMongoClient? _client;
        private IMongoClient? _encryptedClient;
        private DocumentConfiguration? _documents = null;

        protected override async Task<int> ExecuteAsync(CommandContext context, BuildSettings settings, CancellationToken cancellationToken)
        {
            return await AnsiConsole.Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green"))
                .StartAsync("Building Project...", async ctx =>
                {
                    var projectPath = Path.GetFullPath(settings.Project);
                    _documents = await ResourceHelpers.BuildAndGatherResources(projectPath, settings.Environment, cancellationToken);
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

                    ctx.Status("Building and Testing Clients...");

                    (_client, _encryptedClient) = await ClientOperations.CreateClients(_documents);

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

                    ctx.Status("Checking for initial Changes...");
                    var differences = await ClientOperations.GetDifferencesByObject(_client, _documents);

                    if (differences == null)
                    {
                        Console.WriteLine("Error Processing SchemaObject");
                        return 1;
                    }
                    if (differences.ExistingCollections.Count == 0 && differences.NewCollections.Count == 0)
                    {
                        Console.WriteLine("No Collections Found to process");
                    }
                    return 0;
                }
            );
        }
    }
}