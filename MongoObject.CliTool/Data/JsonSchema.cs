using System.Text.Json.Serialization;
using Microsoft.VisualBasic;

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
    public string BsonType {get;set;} = string.Empty;
    [JsonPropertyName("title")]
    public string Title {get;set;} = string.Empty;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("required")]
    public List<string>? Required {get;set;} = null;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("properties")]
    public Dictionary<string, JsonProperty>? Properties {get;set;}
}

internal class JsonProperty
{
    [JsonPropertyName("bsonType")]
    public string BsonType {get;set;} = string.Empty;
    [JsonPropertyName("description")]
    public string Description {get;set;} = string.Empty;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("required")]
    public List<string>? Required {get;set;} = null;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("properties")]
    public Dictionary<string, JsonProperty>? Properties {get;set;} = null;
}