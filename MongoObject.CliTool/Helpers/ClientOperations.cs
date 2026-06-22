using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using DnsClient.Protocol;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Driver.Encryption;
using MongoObject.CliTool.Data;
using MongoObject.Core.Data;
using Spectre.Console;

namespace MongoObject.CliTool.Helpers
{
    public static class ClientOperations
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

        public static async Task<CollectionDifferences?> GetDifferencesByObject(IMongoClient client, DocumentConfiguration documents)
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

            AnsiConsole.MarkupLine($"[green]Found [/][yellow]{databaseNames.Count}[/] [green]databases: [/][yellow]{string.Join(", ", databaseNames)}[/]");
            // everything below is just debug output to test my information gathering
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

            return new CollectionDifferences(existingCollections, newCollections);
        }
    }
}