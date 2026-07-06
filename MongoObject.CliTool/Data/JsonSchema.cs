using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

internal class JsonSchemas : Dictionary<string, JsonSchemaHeader>
{
}

internal class JsonSchemaHeader
{
    [JsonPropertyName("$jsonSchema")]
    public JsonSchema JsonSchema {get;set;} = new();
}

internal class JsonSchema
{
    [JsonPropertyName("bsonType")]
    [BsonElement("bsonType")]
    public string BsonType {get;set;} = string.Empty;
    [JsonPropertyName("title")]
    [BsonElement("title")]
    public string Title {get;set;} = string.Empty;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("required")]
    [BsonElement("required")]
    [BsonIgnoreIfNull]
    public List<string>? Required {get;set;} = null;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("properties")]
    [BsonElement("properties")]
    [BsonIgnoreIfNull]
    public Dictionary<string, JsonProperty>? Properties {get;set;}
}

internal class JsonProperty
{
    [JsonPropertyName("bsonType")]
    [BsonElement("bsonType")]
    public string BsonType {get;set;} = string.Empty;
    [JsonPropertyName("description")]
    [BsonElement("description")]
    public string Description {get;set;} = string.Empty;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("required")]
    [BsonElement("required")]
    [BsonIgnoreIfNull]
    public List<string>? Required {get;set;} = null;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("properties")]
    [BsonElement("properties")]
    [BsonIgnoreIfNull]
    public Dictionary<string, JsonProperty>? Properties {get;set;} = null;
}