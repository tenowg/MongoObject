using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Driver.Encryption;
using MongoObject.PropertyEncryption.Data;
using MongoObject.PropertyEncryption.Interfaces;

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

                // I don't think this needs to be code genned
                builder.Services.AddSingleton<IEncryptedClient>(sp =>
                {
                    var extraOptions = new Dictionary<string, object>
                    {
                        { "cryptSharedLibPath", encryptOptions.MongoCryptDll } // Path to your Automatic Encryption Shared Library
                    };

                    var originalClient = sp.GetService<IMongoClient>();

                    var securedSettings = originalClient is MongoClient concreteClient
                        ? concreteClient.Settings.Clone()
                        : new MongoClientSettings();

                    var kmsProviderCredentials = sp.GetRequiredKeyedService<KmsProvidersDictionary>("KmsProviders");

                    var autoEncryptionOptions = new AutoEncryptionOptions(
                        new CollectionNamespace(encryptOptions.KeyVaultDatabaseName, encryptOptions.KeyVaultCollectionName),
                        kmsProviderCredentials,
                        extraOptions: extraOptions);
                    
                    //var clientSettings = MongoClientSettings.FromConnectionString(encryptOptions.ConnectionString);
                    securedSettings.AutoEncryptionOptions = autoEncryptionOptions;

                    return new EncryptedMongoClient(new MongoClient(securedSettings));
                });

                return builder;
            }
        }
    }
}
