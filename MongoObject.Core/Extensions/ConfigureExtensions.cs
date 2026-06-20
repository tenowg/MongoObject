using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;
using MongoObject.Core.Services;
using MongoOptions.Services;

using System.Security.Cryptography;
using System.Text;

namespace MongoObject.Core.Extensions
{
    public static class ConfigureExtensions
    {
        extension(IServiceCollection services)
        {
            public IServiceCollection AddMongoObject(IConfiguration config, Action<MongoObjectBuilder, MongoObjectOptions> configure)
            {
                var args = Environment.GetCommandLineArgs();
                
                var optionsInstance = new MongoObjectOptions();
                var mongoBuilder = new MongoObjectBuilder(services);
                configure(mongoBuilder, optionsInstance);
                services.AddSingleton(optionsInstance);

                services.AddSingleton<IDocumentKeyManager, MongoDocumentKeyManager>();
                services.AddMemoryCache();
                services.AddSingleton<InternalCacheService>();
                services.AddHostedService<BuildIndexesHostService>();
                services.AddSingleton<MongoServerCapabilities>(sp =>
                {
                    var client = sp.GetRequiredService<IMongoClient>();
                    return MongoServerCapabilities.Resolve(client);
                });

                var objectSerializer = new ObjectSerializer(ObjectSerializer.AllAllowedTypes);
                BsonSerializer.RegisterSerializer(objectSerializer);

                // lets add the Hooks
                foreach(var hook in MongoObjectsPluginRegistry.RegisterDocumentsHook)
                {
                    hook(services, config);
                }

                services.TryAddSingleton<IDistributedLockManager, NoOpLockManager>();

                if (args.Contains("--mongoobject-dump-schema"))
                {
                    Console.WriteLine("Starting");
                    string? envKey = Environment.GetEnvironmentVariable("IPC_AES_KEY");
                    string? envIV = Environment.GetEnvironmentVariable("IPC_AES_IV");
                    if (string.IsNullOrEmpty(envKey) || string.IsNullOrEmpty(envIV))
                    {
                        Console.WriteLine("Keys not found");
                        Environment.Exit(1);
                    }
                    Console.WriteLine("After Key Check...");
                    using var aes = Aes.Create();
                    aes.Key = Convert.FromBase64String(envKey);
                    aes.IV = Convert.FromBase64String(envIV);

                    var bson = MongoObjectsPluginRegistry.SchemaDocument;
                    bson.Add("connection_string", optionsInstance.ConnectionString ?? "");

                    using var encryptor = aes.CreateEncryptor();
                    byte[] plainTextBytes = Encoding.UTF8.GetBytes(bson.ToString());
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(plainTextBytes, 0, plainTextBytes.Length);
                    
                    var outputString = Convert.ToBase64String(encryptedBytes);
                    Console.WriteLine("Sending...");
                    Console.WriteLine($"cli-data: {outputString}");
                    Environment.Exit(0);
                }
                return services;
            }
        }
    }
}
