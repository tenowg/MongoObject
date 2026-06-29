using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Encryption;
using MongoObject.CliTool.Data;
using MongoObject.Core.Data;
using Spectre.Console;

namespace MongoObject.CliTool.Helpers
{
    internal static class ClientOperations
    {
        public async static Task<(IMongoClient standard, IMongoClient? encrypted)> CreateClients(DocumentConfiguration documents)
        {
            var clientSettings = MongoClientSettings.FromUrl(new MongoUrl(documents.ConnectionString));
            var client = new MongoClient(clientSettings);
            IMongoClient? encryptedClient = null;

            if (documents.KmsProviders != null)
            {
                MongoClientSettings.Extensions.AddAutoEncryption();
                var extraOptions = new Dictionary<string, object>
            {
                { "cryptSharedLibPath", documents.MongoCryptPath ?? "" } // Path to your Automatic Encryption Shared Library
            };

                var autoEncryptionOptions = new AutoEncryptionOptions(
                    new CollectionNamespace(documents.KeyVaultDatabaseName, documents.KeyVaultCollectionName),
                    documents.KmsProviders,
                    extraOptions: extraOptions);

                var autoClientSettings = MongoClientSettings.FromUrl(new MongoUrl(documents.ConnectionString));
                autoClientSettings.AutoEncryptionOptions = autoEncryptionOptions;

                encryptedClient = new MongoClient(autoClientSettings);
            }

            // Lets test the clients
            var capabilities = MongoServerCapabilities.Resolve(client);

            return (client, encryptedClient);
        }

        public static async Task<CollectionDifferences?> GetDifferencesByObject(IMongoClient client, DocumentConfiguration documents, bool verbose)
        {
            Dictionary<string, SchemaObject> existingCollections = [];
            Dictionary<string, SchemaObject> newCollections = [];
            List<string> databaseNames = [];

            if (documents.DocumentSchema == null) 
            {    
                return null;
            }

            var databases = documents.DocumentSchema
            .Select(x => new 
            {
                x.Key,
                x.Value,
                DatabaseName = string.IsNullOrWhiteSpace(x.Value.DatabaseName)
                    ? documents.DefaultDatabase
                    : x.Value.DatabaseName,
                x.Value.CollectionName
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.DatabaseName))
            .Where(x => !string.IsNullOrWhiteSpace(x.CollectionName))
            .ToLookup(
                x => x.DatabaseName,
                x => x
            );

            foreach(var databaseName in databases)
            {
                databaseNames.Add(databaseName.Key!);
                var database = client.GetDatabase(databaseName.Key);
                var collectionList = database.ListCollectionNames().ToList();
                var collectionSet = new HashSet<string>(collectionList);

                foreach (var schema in databaseName)
                {
                    schema.Value.DatabaseName = databaseName.Key;

                    if (collectionSet.Contains(schema.Value.CollectionName!))
                    {
                        existingCollections[schema.Key] = schema.Value;
                    }
                    else
                    {
                        newCollections[schema.Key] = schema.Value;
                    }
                }
            }
            if (verbose)
            {
                AnsiConsole.MarkupLine($"[green]Found [/][yellow]{databaseNames.Count}[/] [green]databases: [/][yellow]{string.Join(", ", databaseNames)}[/]");
            
                AnsiConsole.MarkupLine($"[green]Found [/][yellow]{existingCollections.Count}[/] [green]existing collections[/]");
                foreach(var existing in existingCollections)
                {
                    AnsiConsole.MarkupLine($"    [cyan]{existing.Value.DatabaseName}.{existing.Value.CollectionName}[/]");
                }
                AnsiConsole.MarkupLine($"[green]Found [/][yellow]{newCollections.Count}[/] [green]new collections[/]");
                foreach(var newCollection in newCollections)
                {
                    AnsiConsole.MarkupLine($"    [cyan]{newCollection.Value.DatabaseName}.{newCollection.Value.CollectionName}[/]");
                }
            }

            return new CollectionDifferences(existingCollections, newCollections);
        }

        public static async Task<OperationDictionary> ProcessDifferences(IMongoClient standardClient, IMongoClient? encryptedClient, JsonSchemas schemas, CollectionDifferences diffs, CancellationToken cancellationToken = default)
        {
            // var options = new CreateCollectionOptions<object>
            // {
            //     Validator = "",
            //     ValidationAction = DocumentValidationAction.Error,
            //     ValidationLevel = DocumentValidationLevel.Strict
            // };

            // standardClient.GetDatabase("test").CreateCollection("hello", options, cancellationToken);
            var operations = new OperationDictionary();

            foreach(var diff in diffs.ExistingCollections)
            {
                AnsiConsole.WriteLine($"Processing {diff.Key}");
                var database = standardClient.GetDatabase(diff.Value.DatabaseName);
                var jsonSchema = await GetCollectionValidatorSchemaAsync(database, diff.Value.CollectionName!);

                if (jsonSchema != null)
                {
                    // here is where we will check for diffs, if there are any we write renameoperations, remove, or delete operations.
                    AnsiConsole.WriteLine(jsonSchema.ToString());
                }
                else
                {
                    operations[$"{diff.Value.DatabaseName}.{diff.Value.CollectionName}"].Add(new CliOperation("ApplyValidationSchemaOperation")
                    {
                        {"Schema", schemas[diff.Value.CollectionName!]!}  
                    });
                }
            }

            foreach(var diff in diffs.NewCollections)
            {
                operations[$"{diff.Value.DatabaseName}.{diff.Value.CollectionName}"].Add(new CliOperation("CreateCollectionOperation")
                {
                    {"Schema", schemas[diff.Value.CollectionName!]!}  
                });
            }

            return operations;
        }

        public static async Task<BsonDocument?> GetCollectionValidatorSchemaAsync(IMongoDatabase database, string collectionName)
        {
            // Build the listCollections command to find the specific collection
            var command = new BsonDocumentCommand<BsonDocument>(new BsonDocument
            {
                { "listCollections", 1 },
                { "filter", new BsonDocument("name", collectionName) }
            });

            // Execute the command
            var result = await database.RunCommandAsync(command);

            // Navigate to the validator object embedded inside the options
            var cursor = result["cursor"]["firstBatch"].AsBsonArray;
            if (cursor.Count > 0 && cursor[0].AsBsonDocument.Contains("options"))
            {
                var options = cursor[0]["options"].AsBsonDocument;
                if (options.Contains("validator"))
                {
                    return options["validator"].AsBsonDocument;
                }
            }

            return null; // No validator schema found
        }
    }
}