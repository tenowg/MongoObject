using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Encryption;
using MongoObject.CliTool.Data;
using MongoObject.Core.Data;
using Spectre.Console;
using Spectre.Console.Json;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace MongoObject.CliTool.Helpers
{
    internal static class ResourceHelpers
    {
        public const string CliPrefix = "cli-data: ";
        public static async Task<DocumentConfiguration?> BuildAndGatherResources(string projectPath, string environment, bool verbose, CancellationToken cancellationToken = default)
        {
            var flowControl = await BuildProject(projectPath, environment, cancellationToken);
            if (!flowControl)
            {
                return null;
            }

            var execPath = await GetExecPath(projectPath, environment);

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
            
            return await GatherDocument(execPath, workingDir, verbose, cancellationToken);
        }

        private static async Task<DocumentConfiguration?> GatherDocument(string execPath, string workingDir, bool Verbose, CancellationToken cancellationToken = default)
        {   
            using Aes aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();
            string base64Key = Convert.ToBase64String(aes.Key);
            string base64IV = Convert.ToBase64String(aes.IV);

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
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
                    else
                    {
                        AnsiConsole.WriteLine(line);
                    }
                    
                }
            };

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                Console.WriteLine("[RESPONSE ERROR] Project failed to run during Information gathering stage");
                return null;
            }

            if (string.IsNullOrEmpty(base64EncryptedData))
            {
                Console.WriteLine("[RESPONSE ERROR] Process failed to respond with documents. Return value is null");
                return null;
            }

            using var decryptor = aes.CreateDecryptor();
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(base64EncryptedData);
                byte[] plainTextBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                string decryptedString = Encoding.UTF8.GetString(plainTextBytes);
                
                if (Verbose)
                {
                    var jsonWidget = new JsonText(decryptedString);
                    AnsiConsole.Write(jsonWidget);
                }

                return BsonSerializer.Deserialize<DocumentConfiguration>(decryptedString);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        private static async Task<string> GetExecPath(string projectPath, string environment)
        {
            using var targetPath = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    ArgumentList = {"build", projectPath, "-c", environment, "--getProperty:TargetPath"},
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            targetPath.Start();

            // TODO: Fix this using async methods
            string execPath = (await targetPath.StandardOutput.ReadToEndAsync()).Trim();
            string errorOut = (await targetPath.StandardError.ReadToEndAsync()).Trim();

            await targetPath.WaitForExitAsync();

            if (targetPath.ExitCode != 0 || string.IsNullOrEmpty(execPath))
            {
                if (!string.IsNullOrEmpty(errorOut))
                    Console.WriteLine($"[SUBPROCESS ERROR]: {errorOut}");
                return string.Empty;
            }

            return execPath;
        }

        private static async Task<bool> BuildProject(string projectPath, string environment, CancellationToken cancellationToken = default)
        {
            using var build = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    ArgumentList = {"build", projectPath, "-c", environment, "/p:GeneratePackageOnBuild=false", "/p:IsPackable=false", "/nodeReuse:false"},
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            build.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    Console.WriteLine($"[SUBPROCESS ERROR]: {args.Data}");
                }
            };
            build.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    AnsiConsole.MarkupLine(Markup.Escape(args.Data));
                }
            };

            build.Start();
            build.BeginErrorReadLine();
            build.BeginOutputReadLine();
            await build.WaitForExitAsync(cancellationToken);
            if (build.ExitCode != 0)
            {
                AnsiConsole.WriteLine($"[BUILD ERROR]: The source Project ({projectPath}) build failed, please fix any issues and run again.");
                return false;
            }

            return true;
        }
    }
}
