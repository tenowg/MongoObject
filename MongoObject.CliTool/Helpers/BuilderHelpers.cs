using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using MongoDB.Driver;
using MongoObject.CliTool.Data;
using Spectre.Console;
using Spectre.Console.Json;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace MongoObject.CliTool.Helpers
{
    public static class BuilderHelpers
    {
        public static string BuildSchema(CollectionDifferences diffs, IMongoClient standardClient, bool verbose, CancellationToken cancellationToken = default)
        {
            
            var jsonBuilder = new JsonObject();
            foreach (var diff in diffs.NewCollections)
            {
                
                var jsonSchema = jsonBuilder["$jsonSchema"] = new JsonObject();

                jsonSchema["bsonType"] = "object";
                jsonSchema["title"] = "Student Object Validation";
                if (diff.Value.Properties.Any(x => x.IsRequired))
                {
                    jsonSchema["required"]= new JsonArray(string.Join(", ", diff.Value.Properties.Where(x => x.IsRequired).Select(x => x.QueryName).ToList()));
                }
                var properties = new JsonArray();
                foreach(var prop in diff.Value.Properties)
                {
                    var propObject = new JsonObject
                    {
                        [prop.QueryName!] = new JsonObject
                        {
                            ["bsonType"] = prop.BsonType,
                            ["description"] = $"{prop.QueryName} must be a ({prop.BsonType}){(prop.IsRequired ? " and is required." : ".")}"
                        }
                    };
                    properties.Add(propObject);
                }
                jsonSchema["properties"] = properties;
            }

            // this is debug only...
            var test = new JsonText(jsonBuilder.ToString());
            AnsiConsole.Write(test);
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine();

            foreach (var diff in diffs.ExistingCollections)
            {
                
            }
            return jsonBuilder.ToString();
        }
    }
}
#pragma warning restore CS8602 // Dereference of a possibly null reference.