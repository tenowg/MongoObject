using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.PropertyEncryption.Data
{
    public class MongoEncryptionOptions
    {
        public string KmsProviderName { get; set; } = "local";
        public string KeyVaultDatabaseName { get; set; } = "encryption";
        public string KeyVaultCollectionName { get; set; } = "__KeyVault";
        public string ConnectionString { get; set; } = string.Empty;
        public string KeyVaultNamespace
        {
            get
            {
                return KeyVaultDatabaseName + "." + KeyVaultCollectionName;
            }
        }
        public string MongoCryptDll
        {
            get
            {
                if (field == string.Empty)
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    return Path.Combine(baseDir, "mongo_crypt_v1.dll");
                }
                return field;
            }
            set;
        } = string.Empty;
    }
}
