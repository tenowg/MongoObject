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
        public const string CliPrefix = "cli-data: ";
        public static DocumentConfiguration? BuildAndGatherResources(string projectPath, string environment)
        {
            DocumentConfiguration? documents = null;
            var flowControl = BuildProject(projectPath, environment);
            if (!flowControl)
            {
                return null;
            }

            GetExecPath(projectPath, environment, out string execPath);

            if (string.IsNullOrEmpty(execPath))
            {
                Console.WriteLine("ExecutionPath cannot be determined from Project path, ensure the path is correct and try again");
                return null;
            }

            string? workingDir = projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                ? Path.GetDirectoryName(projectPath)
                : projectPath;

            if (workingDir == null)
            {
                Console.WriteLine("WorkingDirectory cannot be determined from Project path");
                return null;
            }
            
            GatherDocument(out documents, execPath, workingDir);

            return documents;
        }

        private static void GatherDocument(out DocumentConfiguration? documents, string execPath, string? workingDir)
        {   
            using Aes aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();
            string base64Key = Convert.ToBase64String(aes.Key);
            string base64IV = Convert.ToBase64String(aes.IV);

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                //Arguments = $"exec \"{execPath}\" --mongoobject-dump-schema",
                ArgumentList = {"exec", execPath, "--mongoobject-dump-schema"},
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir
            };

            startInfo.EnvironmentVariables["IPC_AES_KEY"] = base64Key;
            startInfo.EnvironmentVariables["IPC_AES_IV"] = base64IV;

            using Process process = new() { StartInfo = startInfo };
            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    Console.WriteLine($"[SUBPROCESS ERROR]: {args.Data}");
                }
            };

            string? base64EncryptedData = null;
            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    var line = args.Data;
                    if (line.StartsWith(CliPrefix))
                    {
                        base64EncryptedData = line[CliPrefix.Length..];
                    }
                }
            };

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();

            if (string.IsNullOrEmpty(base64EncryptedData))
            {
                Console.WriteLine("[REASPONSE ERROR] Process failed to respond with documents. Return value is null");
                documents = null;
                return;
            }

            using var decryptor = aes.CreateDecryptor();
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(base64EncryptedData);
                byte[] plainTextBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                string decryptedString = Encoding.UTF8.GetString(plainTextBytes);
            
                documents = BsonSerializer.Deserialize<DocumentConfiguration>(decryptedString);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                documents = null;
            }
        }

        private static void GetExecPath(string projectPath, string environment, out string execPath)
        {
            using var targetPath = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    //Arguments = $"build \"{projectPath}\" -c {environment} --getProperty:TargetPath",
                    ArgumentList = {"build", projectPath, "-c", environment, "--getProperty:TargetPath"},
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            targetPath.Start();

            // TODO: Fix this using async methods
            execPath = targetPath.StandardOutput.ReadToEnd().Trim();
            string errorOut = targetPath.StandardError.ReadToEnd().Trim();

            targetPath.WaitForExit();

            if (targetPath.ExitCode != 0 || string.IsNullOrEmpty(execPath))
            {
                if (!string.IsNullOrEmpty(errorOut))
                    Console.WriteLine($"[SUBPROCESS ERROR]: {errorOut}");
                return;
            }

            return;
        }

        private static bool BuildProject(string projectPath, string environment)
        {
            using var build = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    //Arguments = $"build \"{projectPath}\" -c {environment} /p:GeneratePackageOnBuild=false /p:IsPackable=false",
                    ArgumentList = {"build", projectPath, "-c", environment, "/p:GeneratePackageOnBuild=false", "/p:IsPackable=false"},
                    RedirectStandardError = true
                }
            };
            build.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    Console.WriteLine($"[SUBPROCESS ERROR]: {args.Data}");
                }
            };

            build.Start();
            build.BeginErrorReadLine();
            build.WaitForExit();
            if (build.ExitCode != 0)
            {
                Console.WriteLine($"[BUILD ERROR]: The source Project ({projectPath}) build failed, please fix any issues and run again.");
                return false;
            }

            return true;
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
