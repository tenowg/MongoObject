using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoObject.CliTool.Data
{
    [BsonIgnoreExtraElements]
    public class DocumentConfiguration
    {
        [BsonElement("connection_string")]
        public string? ConnectionString { get; set; }

        [BsonElement("documentSchema")]
        public Dictionary<string, SchemaObject>? DocumentSchema { get; set; }

        [BsonElement("kmsProviders")]
        public KmsProvidersDictionary? KmsProviders { get; set; }
    }

    public class SchemaObject
    {
        [BsonElement("properties")]
        public List<SchemaProperty> Properties { get; set; }

        [BsonElement("is_encrypted")]
        public bool IsEncrypted { get; set; }

        [BsonElement("collection_name")]
        public string CollectionName { get; set; }

        [BsonElement("database_name")]
        public string DatabaseName { get; set; }
    }

    public class SchemaProperty
    {
        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("queryName")]
        public string QueryName { get; set; }

        [BsonElement("isEncrypted")]
        public bool IsEncrypted { get; set; }
    }
}