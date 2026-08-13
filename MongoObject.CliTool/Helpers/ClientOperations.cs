using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Encryption;
using MongoObject.CliTool.Data;
using MongoObject.Core.Data;
using SharpCompress.Compressors.Filters;
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

        public static async Task<OperationDictionary> ProcessDifferences(IMongoClient standardClient, Dictionary<string, List<IndexObject>> indexes, JsonSchemas schemas, CollectionDifferences diffs, CancellationToken cancellationToken = default)
        {
            var operations = new OperationDictionary();

            foreach(var diff in diffs.ExistingCollections)
            {
                AnsiConsole.WriteLine($"Processing {diff.Key}");
                var database = standardClient.GetDatabase(diff.Value.DatabaseName);
                var jsonSchema = await GetCollectionValidatorSchemaAsync(database, diff.Value.CollectionName!);
                var newSchema = schemas[diff.Value.CollectionName!].JsonSchema.ToBsonDocument();

                // if a schema was reteived process difference if not add one, as it is likely a new collection.
                if (jsonSchema != null)
                {
                    // here is where we will check for diffs, if there are any we write renameoperations, remove, or delete operations.
                    var existingProperties = jsonSchema.GetValue("$jsonSchema", new BsonDocument()).AsBsonDocument.GetValue("properties", new BsonDocument()).AsBsonDocument;
                    var newProperties = newSchema.GetValue("properties", new BsonDocument()).AsBsonDocument; //.GetValue("properties").AsBsonDocument;
                    var schemaDiff = CompareSchemas(existingProperties, newProperties);
                    if (schemaDiff == null || (schemaDiff.AddedFields.Count == 0 && schemaDiff.RemovedFields.Count == 0 && schemaDiff.ChangedFields.Count == 0))
                    {
                        AnsiConsole.WriteLine("No changes detected, not appling schema");
                        var (indexRemove, indexAdd) = await ProcessIndexes(standardClient, indexes, schemas, diff.Value, cancellationToken);
                        foreach(var kvp in indexRemove)
                        {
                            foreach(var op in kvp.Value)
                            {
                                operations[$"{diff.Value.DatabaseName}.{diff.Value.CollectionName}"].Add(op);
                            }
                        }
                        foreach (var kvp in indexAdd)
                        {
                            foreach (var op in kvp.Value)
                            {
                                operations[$"{diff.Value.DatabaseName}.{diff.Value.CollectionName}"].Add(op);
                            }
                        }
                    }
                    else
                    {
                        foreach (var changed in schemaDiff.ChangedFields)
                            Console.WriteLine($"[CHANGED] {changed} — will be updated.");

                        var resolutions = ResolveMigrationIntent(schemaDiff);

                        var resOperations = new OperationDictionary();

                        // now we go thru each change and make the right operation
                        foreach (var kvp in resolutions)
                        {
                            var delete = false;
                            switch(kvp.Value)
                            {
                                case "new":
                                    AnsiConsole.WriteLine($"The new property {kvp.Key} will be added.");
                                    break;
                                case "removed":
                                    AnsiConsole.WriteLine($"The property {kvp.Key} will be removed");
                                    switch(diff.Value.MigrationPolicy)
                                    {
                                        case "AlwaysAsk":
                                            // create interactive script
                                            delete = AnsiConsole.Confirm($"{kvp.Key} has been removed from the POCO ({diff.Key}), do you want to delete the field's data?");
                                            break;
                                        case "Ignore":
                                            // its fine, do nothing
                                            break;
                                        case "Warn":
                                            AnsiConsole.MarkupLine($"[yellow]{kvp.Key} will be Orphaned Data, to force deletion use [/][red]MigrationSchemaAttribute[/][yellow] and set the policy to Delete[/]");
                                            break;
                                        case "Delete":
                                            delete = true;
                                            break;
                                    }
                                    if (delete)
                                    {
                                        // remove the field by setting the operation
                                        resOperations[$"{diff.Value.DatabaseName}.{diff.Value.CollectionName}"].Add(new CliOperation("DeletePropertyOperation")
                                        {
                                             {"Property", kvp.Key} 
                                        });
                                    }
                                    break;
                                default:
                                    AnsiConsole.WriteLine($"The property {kvp.Value} will be renamed to {kvp.Key}");
                                    resOperations[$"{diff.Value.DatabaseName}.{diff.Value.CollectionName}"].Add(new CliOperation("RenamePropertyOperation")
                                    {
                                        {"From", kvp.Value},
                                        {"To", kvp.Key}
                                    });
                                    break;
                            }
                        }

                        // we will move this whole checking of operations to later.
                        var (indexRemove, indexAdd) = await ProcessIndexes(standardClient, indexes, schemas, diff.Value, cancellationToken);

                        if (resOperations.Count > 0)
                        {
                            operations[$"{diff.Value.DatabaseName}.{diff.Value.CollectionName}"].Add(new CliOperation("DisableValidation")
                            {
                                {"Reason", "Disabling validation to allow structural transformations before applying the new schema."}
                            });
                        }

                        foreach (var kvp in indexRemove)
                        {
                            foreach (var op in kvp.Value)
                            {
                                operations[$"{diff.Value.DatabaseName}.{diff.Value.CollectionName}"].Add(op);
                            }
                        }

                        foreach (var kvp in resOperations)
                        {
                            foreach(var op in kvp.Value)
                            {
                                operations[$"{diff.Value.DatabaseName}.{diff.Value.CollectionName}"].Add(op);
                            }
                        }

                        foreach(var kvp in indexAdd)
                        {
                            foreach(var op in kvp.Value)
                            {
                                operations[$"{diff.Value.DatabaseName}.{diff.Value.CollectionName}"].Add(op);
                            }
                        }
                        
                        operations[$"{diff.Value.DatabaseName}.{diff.Value.CollectionName}"].Add(new CliOperation("ApplyValidationSchemaOperation")
                        {
                            {"Schema", schemas[diff.Value.CollectionName!]!}  
                        });
                    }
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

        public static async Task<(OperationDictionary remove, OperationDictionary add)> ProcessIndexes(IMongoClient standardClient, Dictionary<string, List<IndexObject>> indexes, JsonSchemas schema, SchemaObject diff, CancellationToken cancellationToken = default)
        {
            var indexRemoveOperations = new OperationDictionary();
            var indexAddOperations = new OperationDictionary();
            var database = standardClient.GetDatabase(diff.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(diff.CollectionName);
            

            AnsiConsole.WriteLine($"Processing {diff.DatabaseName}.{diff.CollectionName}");
            using var cursor = await collection.Indexes.ListAsync(cancellationToken);
            var indexList = await cursor.ToListAsync(cancellationToken: cancellationToken);
            var mongoIndex = indexList.Select(x => BsonSerializer.Deserialize<MongoIndex>(x));
            if (!indexes.TryGetValue($"{diff.DatabaseName}.{diff.CollectionName}", out List<IndexObject>? indexObjects))
            {
                indexObjects ??= []; 
            }
            var tempIndex = new List<IndexObject>(indexObjects);

            foreach (var index in mongoIndex)
            {
                Console.WriteLine(index.Name);
                bool delete = false;
                // this is just to ensure structure
                if (index.Name != "_id_")
                {
                    // decide to delete index
                    if (!indexObjects.Any(x => x.IndexName == index.Name))
                    {
                        // does not contain the index, delete it
                        delete = true;
                    }
                    else
                    {
                        var indexObject = indexObjects.FirstOrDefault(x => x.IndexName == index.Name);
                        var properties = new Dictionary<string, string>();
                        foreach (var prop in indexObject!.Entities)
                            properties.Add(prop.Key, prop.Value);

                        // Keys in schema but missing from live index → need to be added
                        var added = indexObject.Entities.Keys
                            .Where(k => !index.Keys.ContainsKey(k))
                            .ToList();

                        // Keys in live index but missing from schema → need to be removed
                        var removed = index.Keys.Keys
                            .Where(k => !indexObject.Entities.ContainsKey(k))
                            .ToList();
                        if (added.Count > 0 || removed.Count > 0)
                        {
                            AnsiConsole.WriteLine($"Index diff on '{index.Name}': Added=[{string.Join(", ", added)}] Removed=[{string.Join(", ", removed)}]");

                            indexRemoveOperations[$"{diff.DatabaseName}.{diff.CollectionName}"].Add(new CliOperation("DropIndexOperation")
                            {
                                { "IndexName", indexObject.IndexName },
                                {"Reason", $"Index diff on {index.Name}: Added=[{string.Join(", ", added)}] Removed=[{string.Join(", ", removed)}]"}
                            });

                            indexAddOperations[$"{diff.DatabaseName}.{diff.CollectionName}"].Add(new CliOperation("CreateIndexOperation")
                            {
                                { "IndexName", indexObject.IndexName },
                                { "Members", properties },
                                { "Unique", indexObject.Unique }
                            });
                        }

                        tempIndex.Remove(indexObject);
                    }

                    if (delete)
                    {
                        if (index.Name.StartsWith("__") || index.Name.Contains("__safeContent__")) continue;

                        // 3. Ignore search helper configurations
                        if (index.Name.StartsWith("fts") || index.Name.StartsWith("text"))
                        {
                            // Only drop if you explicitly created and named them
                            continue;
                        }
                        indexRemoveOperations[$"{diff.DatabaseName}.{diff.CollectionName}"].Add(new CliOperation("DropIndexOperation")
                        {
                            { "IndexName", index.Name },
                            {"Reason", $"Index ({index.Name}) not found on {diff.DatabaseName}.{diff.CollectionName}"}
                        });
                    }
                }
            }

            foreach(var index in tempIndex)
            { 
                var properties = new Dictionary<string, string>();
                foreach (var prop in index!.Entities)
                {
                    properties.Add(prop.Key, prop.Value);
                }

                indexAddOperations[$"{diff.DatabaseName}.{diff.CollectionName}"].Add(new CliOperation("CreateIndexOperation")
                    {
                        { "IndexName", index.IndexName },
                        { "Members", properties },
                        { "Unique", index.Unique }
                    });
            }

            return (indexRemoveOperations, indexAddOperations);
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

        public static SchemaDiff CompareSchemas(BsonDocument existingProps, BsonDocument newProps, string path = "")
        {
            var diff = new SchemaDiff();

            // --- Detect Added and Changed ---
            foreach (var property in newProps)
            {
                string currentPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";

                if (!existingProps.Contains(property.Name))
                {
                    diff.AddedFields.Add(currentPath);
                }
                else
                {
                    var oldVal = existingProps[property.Name];
                    var newVal = property.Value;

                    if (!oldVal.Equals(newVal))
                    {
                        // Recurse into nested objects
                        if (oldVal.IsBsonDocument && newVal.IsBsonDocument &&
                            oldVal.AsBsonDocument.Contains("properties") &&
                            newVal.AsBsonDocument.Contains("properties"))
                        {
                            var nestedDiff = CompareSchemas(
                                oldVal.AsBsonDocument["properties"].AsBsonDocument,
                                newVal.AsBsonDocument["properties"].AsBsonDocument,
                                currentPath
                            );

                            diff.AddedFields.AddRange(nestedDiff.AddedFields);
                            diff.RemovedFields.AddRange(nestedDiff.RemovedFields);
                            diff.ChangedFields.AddRange(nestedDiff.ChangedFields);
                        }
                        else
                        {
                            diff.ChangedFields.Add(currentPath);
                        }
                    }
                }
            }

            // --- Detect Removed ---
            foreach (var property in existingProps)
            {
                string currentPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";

                if (!newProps.Contains(property.Name))
                {
                    diff.RemovedFields.Add(currentPath);
                }
            }

            return diff;
        }

        public static Dictionary<string, string> ResolveMigrationIntent(SchemaDiff diff)
        {
            // Key = new field path, Value = resolved intent ("new" or the old field path it was renamed from)
            var resolutions = new Dictionary<string, string>();

            foreach (var addedField in diff.AddedFields)
            {
                AnsiConsole.WriteLine($"\n[?] Field '{addedField}' appears to be new.");

                if (diff.RemovedFields.Any())
                {
                    var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                        .Title("Select a [green]Property[/]:")
                        .AddChoices("New field")
                        .AddChoices(diff.RemovedFields));
                    // Console.WriteLine("    Is this a new field, or was it renamed from an existing one?");
                    // Console.WriteLine("    [0] New field");

                    // for (int i = 0; i < diff.RemovedFields.Count; i++)
                    // {
                    //     Console.WriteLine($"    [{i + 1}] Renamed from '{diff.RemovedFields[i]}'");
                    // }

                    //Console.Write("    Enter choice: ");
                    //var input = Console.ReadLine();

                    //if (int.TryParse(input, out int choice) && choice > 0 && choice <= diff.RemovedFields.Count)
                    //{
                    if (choice != "New field")
                    {
                        //var renamedFrom = diff.RemovedFields[choice - 1];
                        resolutions[addedField] = choice;
                        AnsiConsole.WriteLine($"    -> Marked as rename: '{choice}' -> '{addedField}'");
                    }
                    else
                    {
                        resolutions[addedField] = "new";
                        AnsiConsole.WriteLine($"    -> Marked as new field.");
                    }
                }
                else
                {
                    resolutions[addedField] = "new";
                    AnsiConsole.WriteLine($"    -> No removed fields to match against. Marked as new.");
                }
            }

            // Any removed fields not claimed as a rename source are true removals
            var claimedRemovals = resolutions.Values.Where(v => v != "new").ToHashSet();
            foreach (var removedField in diff.RemovedFields.Where(r => !claimedRemovals.Contains(r)))
            {
                Console.WriteLine($"\n[REMOVED] '{removedField}' was not matched to any new field. It will be treated as removed.");
                resolutions[removedField] = "removed";
            }

            return resolutions;
        }

    }
}