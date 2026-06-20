using System.Diagnostics;
using CliTool;
using MongoDB.Bson.Serialization;
using MongoObject.CliTool.Data;
using System.Security.Cryptography;
using System.Text;
using MongoDB.Driver;

namespace MongoObject.CliTool.Processors
{
    internal class MigrationProcessor : IProcessor
    {
        private IMongoClient _client;
        private IMongoClient _encryptedClient;
        private string _projectFullPath;

        public void Execute(Settings settings)
        {
            if (settings.Action == MigrationOptions.Migrate)
            {
                if (settings.Build)
                {
                    var documents = GatherResources(settings);
                    if (documents == null)
                    {
                        Console.WriteLine("There was an error retrieving the Document Schema");
                        return;
                    }
                    if (documents.ConnectionString == null)
                    {
                        Console.WriteLine("Please be sure to provide a connection String in you package configuation.");
                        return;
                    }
                    CreateClients(settings, documents);
                }
                else if (settings.Migrate)
                {
                    
                }
            }
        }

        private void CreateClients(Settings settings, DocumentConfiguration documents)
        {
            var clientSettings = MongoClientSettings.FromUrl(new MongoUrl(documents.ConnectionString));
            _client = new MongoClient(clientSettings);

            if (documents.KmsProviders != null)
            {
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

                _encryptedClient = new MongoClient(autoClientSettings);
            }
        }

        private DocumentConfiguration? GatherResources(Settings settings)
        {
            DocumentConfiguration? documents = null;
            _projectFullPath = Path.GetFullPath(settings.Project);

            var build = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build {settings.Project} -c Debug /p:GeneratePackageOnBuild=false /p:IsPackable=false"
                }
            };

            build.Start();
            build.WaitForExit();
            var code = build.ExitCode;

            if (code != 0)
            {
                return documents;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project {settings.Project} --no-build -- --mongoobject-dump-schema",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _projectFullPath
            };

            using var aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();
            string base64Key = Convert.ToBase64String(aes.Key);
            string base64IV = Convert.ToBase64String(aes.IV);

            startInfo.EnvironmentVariables["IPC_AES_KEY"] = base64Key;
            startInfo.EnvironmentVariables["IPC_AES_IV"] = base64IV;

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            while (!process.StandardOutput.EndOfStream)
            {
                
                string? line = process.StandardOutput.ReadLine();
                Console.WriteLine(line);
                if (line != null && line.StartsWith("cli-data: "))
                {
                    var base64EncryptedData = line["cli-data:".Length..];

                    byte[] encryptedBytes = Convert.FromBase64String(base64EncryptedData);
                    using var decryptor = aes.CreateDecryptor();
                    byte[] plainTextBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                    string decryptedString = Encoding.UTF8.GetString(plainTextBytes);
                    Console.WriteLine(decryptedString);
                    documents = BsonSerializer.Deserialize<DocumentConfiguration>(decryptedString);
                }
            }
            process.WaitForExit();
            return documents;
        }
    }
}