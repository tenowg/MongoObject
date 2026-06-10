using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Driver.Encryption;
using MongoObject.PropertyEncryption.Data;

namespace MongoObject.Core.Extensions
{
    public static class SetupExtensions
    {
        extension(MongoObjectBuilder builder)
        {
            public MongoObjectBuilder AddMongoEncryption(Action<MongoEncryptionOptions> options)
            {
                var encryptOptions = new MongoEncryptionOptions();
                options.Invoke(encryptOptions);
                builder.Services.AddSingleton(encryptOptions);
                MongoClientSettings.Extensions.AddAutoEncryption();

                builder.Services.AddKeyedSingleton<Dictionary<string, IReadOnlyDictionary<string, object>>>("KmsProviders", (sp, _) =>
                {
                    var kmsProviderCredentials = new Dictionary<string, IReadOnlyDictionary<string, object>>();
                    try
                    {
                        var localCustomerMasterKeyBytes = File.ReadAllBytes("crypt-master.key.bin");
                        if (localCustomerMasterKeyBytes.Length != 96)
                        {
                            throw new Exception("Expected the customer master key file to be 96 bytes.");
                        }
                        var localOptions = new Dictionary<string, object>
                        {
                            { "key", localCustomerMasterKeyBytes }
                        };
                        kmsProviderCredentials.Add("local", localOptions);
                    }
                    catch
                    {
                        throw;
                    }

                    return kmsProviderCredentials;
                });

                builder.Services.AddKeyedSingleton<IMongoClient>("SecuredClient", (sp, _) =>
                {
                    var extraOptions = new Dictionary<string, object>
                    {
                        { "cryptSharedLibPath", encryptOptions.MongoCryptDll } // Path to your Automatic Encryption Shared Library
                    };

                    var kmsProviderCredentials = sp.GetRequiredKeyedService<Dictionary<string, IReadOnlyDictionary<string, object>>>("KmsProviders");

                    var autoEncryptionOptions = new AutoEncryptionOptions(
                        new CollectionNamespace(encryptOptions.KeyVaultDatabaseName, encryptOptions.KeyVaultCollectionName),
                        kmsProviderCredentials,
                        extraOptions: extraOptions);
                    
                    var clientSettings = MongoClientSettings.FromConnectionString(encryptOptions.ConnectionString);
                        clientSettings.AutoEncryptionOptions = autoEncryptionOptions;

                    return new MongoClient(clientSettings);
                });

                return builder;
            }
        }
    }
}
