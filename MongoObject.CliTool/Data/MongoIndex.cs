using MongoDB.Bson.Serialization.Attributes;

namespace MongoObject.CliTool.Data
{
    [BsonIgnoreExtraElements]
    public class MongoIndex
    {
        [BsonElement("v")]
        public int Version { get; set; } = 1;
        [BsonElement("key")]
        public Dictionary<string, object> Keys { get; set; } = [];
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;
        [BsonElement("unique")]
        public bool Unique { get; set; } = false;
    }
}