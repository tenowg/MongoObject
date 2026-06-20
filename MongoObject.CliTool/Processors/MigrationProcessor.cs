using System.Diagnostics;
using CliTool;
using MongoDB.Bson.Serialization;
using MongoObject.CliTool.Data;
using System.Security.Cryptography;
using System.Text;

namespace MongoObject.CliTool.Processors
{
    internal class MigrationProcessor : IProcessor
    {
        public void Execute(Settings settings)
        {
            if (settings.Action == MigrationOptions.Migrate)
            {
                if (settings.Build)
                {
                    GatherResources(settings);
                }
                else if (settings.Migrate)
                {
                    
                }
            }
        }

        private void GatherResources(Settings settings)
        {
            string projectFullPath = Path.GetFullPath(settings.Project);

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
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project {settings.Project} --no-build -- --mongoobject-dump-schema",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = projectFullPath
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
                DocumentConfiguration? documents = null;
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
        }
    }
}