using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using DnsClient.Protocol;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Driver.Encryption;
using MongoObject.CliTool.Data;
using MongoObject.Core.Data;

namespace MongoObject.CliTool.Helpers
{
    public static class ClientOperations
    {
        public static (IMongoClient standard, IMongoClient? encrypted) CreateClients(DocumentConfiguration documents)
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

        public static void GetDifferencesByObject(IMongoClient client,
                                                  DocumentConfiguration documents,
                                                  out ILookup<string, KeyValuePair<string, SchemaObject>>? databases,
                                                  out Dictionary<string, SchemaObject> existingCollections,
                                                  out Dictionary<string, SchemaObject> newCollections)
        {
            existingCollections = [];
            newCollections = [];
            databases = null;

            if (documents.DocumentSchema == null) 
            {    
                return;
            }

            //databases = documents.DocumentSchema.Select(x => x.Value.DatabaseName ?? documents.DefaultDatabase)
            //    .Where(x => !string.IsNullOrWhiteSpace(x)).DistinctBy(x => x).ToList();
            //    databases.Add(documents.DefaultDatabase);
            databases = documents.DocumentSchema.ToLookup(
                x => string.IsNullOrEmpty(x.Value.DatabaseName) ? documents.DefaultDatabase ?? "" : x.Value.DatabaseName,
                x => x
                );

            foreach(var databaseName in databases)
            {
                Console.WriteLine($"|{databaseName.Key}|");
                var database = client.GetDatabase(databaseName.Key);
                var collectionList = database.ListCollectionNames().ToList();
                var collectionSet = new HashSet<string>(collectionList);

                existingCollections = databaseName
                    .Where(x => x.Value.CollectionName != null)
                    // null indicates that the databasename == using the default databasename and not a custom databasename
                    .Where(x => x.Value.DatabaseName == databaseName.Key || (string.IsNullOrEmpty(x.Value.DatabaseName) && databaseName.Key == documents.DefaultDatabase))
                    .Where(x => 
                    {
                        x.Value.DatabaseName = string.IsNullOrWhiteSpace(x.Value.DatabaseName) ? databaseName.Key : x.Value.DatabaseName;
                        return collectionSet.Contains(x.Value.CollectionName!);
                    }).Concat(existingCollections).ToDictionary();

                newCollections = databaseName
                    .Where(x => x.Value.DatabaseName == databaseName.Key || (string.IsNullOrEmpty(x.Value.DatabaseName) && databaseName.Key == documents.DefaultDatabase))
                    .Where(x => 
                    { 
                        // this has to be done, because when the data from the main process arrives it doesn't have the opurtunity to add the the default
                        // database to the individual objects.
                        x.Value.DatabaseName = string.IsNullOrWhiteSpace(x.Value.DatabaseName) ? databaseName.Key : x.Value.DatabaseName;
                        return !collectionSet.Contains(x.Value.CollectionName!); 
                    }).Concat(newCollections).ToDictionary();
            }

            // everything below is just debug output to test my information gathering
            Console.WriteLine("Existing Databases we will check properties next");
            foreach(var existing in existingCollections)
            {
                Console.WriteLine(existing.Value.CollectionName + " -- " + existing.Value.DatabaseName);
            }
            Console.WriteLine("New Collections, we will do nothing with them, jsut list them");
            foreach(var newCollection in newCollections)
            {
                Console.WriteLine(newCollection.Value.CollectionName + " -- " + newCollection.Value.DatabaseName);
            }
        }
    }
}