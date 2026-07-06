
using System.Security.Cryptography;
using System.Text;
using MongoObject.Core.Data;
using MongoObject.Core.Extensions;

internal static class CliHooks
{
    public static async Task ExecuteAsync(MongoObjectOptions options)
    {
        var args = Environment.GetCommandLineArgs();
        
        if (args.Contains("--mongoobject-dump-schema"))
        {
            MongoBuildPath(options);
        }

        if (args.Contains("--mongoobject-run-migration"))
        {
            // we will do this differently
            var bson = MongoObjectsPluginRegistry.SchemaDocument;
            bson.Add("connection_string", options.ConnectionString ?? "");
            bson.Add("default_database", options.DatabaseName);
            bson.Add("migration_folder", options.MigrationFolder);

            await MongoRunMigration();
        }
    }

    private static async Task MongoRunMigration()
    {
        Console.WriteLine("----------------------------------------------------------------------------------------------");
        try 
        {
            await MongoObjectsPluginRegistry.RunMigrations();
        } 
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        Environment.Exit(0);
    }

    private static void MongoBuildPath(MongoObjectOptions options)
    {
        string? envKey = Environment.GetEnvironmentVariable("IPC_AES_KEY");
        string? envIV = Environment.GetEnvironmentVariable("IPC_AES_IV");
        if (string.IsNullOrEmpty(envKey) || string.IsNullOrEmpty(envIV))
        {
            Environment.Exit(1);
        }
        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(envKey);
        aes.IV = Convert.FromBase64String(envIV);

        var bson = MongoObjectsPluginRegistry.SchemaDocument;
        bson.Add("connection_string", options.ConnectionString ?? "");
        bson.Add("default_database", options.DatabaseName);
        bson.Add("migration_folder", options.MigrationFolder);

        using var encryptor = aes.CreateEncryptor();
        byte[] plainTextBytes = Encoding.UTF8.GetBytes(bson.ToString());
        byte[] encryptedBytes = encryptor.TransformFinalBlock(plainTextBytes, 0, plainTextBytes.Length);
        
        var outputString = Convert.ToBase64String(encryptedBytes);
        Console.WriteLine($"cli-data: {outputString}");
        Environment.Exit(0);
    }
}