using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Encryption;
using MongoObject.CliTool.Data;
using MongoObject.Core.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace MongoObject.CliTool.Helpers
{
    internal static class ResourceHelpers
    {
        public static DocumentConfiguration? BuildAndGatherResources(string projectPath)
        {
            DocumentConfiguration? documents = null;

            var build = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build {projectPath} -c Debug /p:GeneratePackageOnBuild=false /p:IsPackable=false"
                }
            };

            build.Start();
            build.WaitForExit();
            var code = build.ExitCode;

            if (code != 0)
            {
                return null;
            }

            string execPath = string.Empty;
            
            using (var targetPath = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{projectPath}\" --getProperty:TargetPath",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            })
            {
                targetPath.Start();
                
                // FIX: Synchronous read prevents race conditions with WaitForExit()
                execPath = targetPath.StandardOutput.ReadToEnd().Trim();
                string errorOut = targetPath.StandardError.ReadToEnd().Trim();
                
                targetPath.WaitForExit();

                if (targetPath.ExitCode != 0 || string.IsNullOrEmpty(execPath))
                {
                    if (!string.IsNullOrEmpty(errorOut))
                        Console.WriteLine($"[SUBPROCESS ERROR]: {errorOut}");
                    return null;
                }
            }

            string? workingDir = projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) 
                ? Path.GetDirectoryName(projectPath) 
                : projectPath;

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                //Arguments = $"run --project {projectPath} --no-build -- --mongoobject-dump-schema",
                Arguments = $"exec \"{execPath}\" --mongoobject-dump-schema",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir
            };

            using var aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();
            string base64Key = Convert.ToBase64String(aes.Key);
            string base64IV = Convert.ToBase64String(aes.IV);

            startInfo.EnvironmentVariables["IPC_AES_KEY"] = base64Key;
            startInfo.EnvironmentVariables["IPC_AES_IV"] = base64IV;

            using var process = new Process { StartInfo = startInfo };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    Console.WriteLine($"[SUBPROCESS ERROR]: {args.Data}");
                }
            };

            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    var line = args.Data;
                    if (line != null && line.StartsWith("cli-data: "))
                    {
                        var base64EncryptedData = line["cli-data:".Length..];

                        byte[] encryptedBytes = Convert.FromBase64String(base64EncryptedData);
                        using var decryptor = aes.CreateDecryptor();
                        byte[] plainTextBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        string decryptedString = Encoding.UTF8.GetString(plainTextBytes);
                        Console.WriteLine(decryptedString); // <------- Debug purposes only REMOVE THIS
                        documents = BsonSerializer.Deserialize<DocumentConfiguration>(decryptedString);
                    }
                }
            };

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();

            return documents;
        }

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
    }
}
