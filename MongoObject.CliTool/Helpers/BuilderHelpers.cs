using System.Text.Json.Nodes;
using MongoDB.Driver;
using MongoObject.CliTool.Data;
using Spectre.Console;
using Spectre.Console.Json;

namespace MongoObject.CliTool.Helpers
{
    public static class BuilderHelpers
    {
        public static JsonObject BuildSchema(CollectionDifferences diffs, IMongoClient standardClient, bool verbose, CancellationToken cancellationToken = default)
        {
            var jsonBuilder = new JsonObject();

            foreach (var diff in diffs.NewCollections)
            {
                jsonBuilder[diff.Value.CollectionName ?? diff.Key] = BuildCollectionValidator(diff.Value);
            }

            foreach (var diff in diffs.ExistingCollections)
            {
                jsonBuilder[diff.Value.CollectionName ?? diff.Key] = BuildCollectionValidator(diff.Value);
            }
 
            if (verbose)
            {
                var test = new JsonText(jsonBuilder.ToString());
                AnsiConsole.Write(test);
                AnsiConsole.WriteLine();
            }

            return jsonBuilder;
        }

        private static JsonObject BuildCollectionValidator(SchemaObject schema)
        {
            var jsonSchema = new JsonObject
            {
                ["bsonType"] = schema.BsonType,
                ["title"] = $"{schema.Name} Object Validation",
                ["properties"] = BuildProperties(schema.Properties)
            };

            var required = BuildRequired(schema.Properties);
            if (required.Count > 0)
            {
                jsonSchema["required"] = required;
            }

            return new JsonObject
            {
                ["$jsonSchema"] = jsonSchema,
                //["database"] = schema.DatabaseName
            };
        }

        private static JsonArray BuildRequired(IEnumerable<SchemaProperty> properties)
        {
            var required = new JsonArray();

            foreach (var property in properties.Where(x => x.IsRequired && !string.IsNullOrWhiteSpace(x.QueryName)))
            {
                required.Add(property.QueryName);
            }

            return required;
        }

        private static JsonObject BuildProperties(IEnumerable<SchemaProperty> properties)
        {
            var jsonProperties = new JsonObject();

            foreach (var property in properties.Where(x => !string.IsNullOrWhiteSpace(x.QueryName)))
            {
                jsonProperties[$"Document.{property.QueryName!}"] = new JsonObject
                {
                    ["bsonType"] = property.BsonType,
                    ["description"] = $"{property.QueryName} must be a ({property.BsonType}){(property.IsRequired ? " and is required." : ".")}"
                };
            }

            return jsonProperties;
        }
    }
}