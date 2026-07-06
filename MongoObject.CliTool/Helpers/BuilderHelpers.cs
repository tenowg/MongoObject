using System.Text.Json;
using MongoDB.Driver;
using MongoObject.CliTool.Data;
using Spectre.Console;
using Spectre.Console.Json;

namespace MongoObject.CliTool.Helpers
{
    internal static class BuilderHelpers
    {
        public static JsonSchemas BuildSchema(CollectionDifferences diffs, IMongoClient standardClient, bool verbose, CancellationToken cancellationToken = default)
        {
            var jsonBuilder = new JsonSchemas();

            foreach (var diff in diffs.NewCollections)
            {
                jsonBuilder[diff.Value.CollectionName ?? diff.Key] = BuildCollectionValidator(diff.Value, diffs);
            }

            foreach (var diff in diffs.ExistingCollections)
            {
                jsonBuilder[diff.Value.CollectionName ?? diff.Key] = BuildCollectionValidator(diff.Value, diffs);
            }
 
            if (verbose)
            {
                var json = JsonSerializer.Serialize(jsonBuilder);
                var test = new JsonText(json);
                AnsiConsole.Write(test);
                AnsiConsole.WriteLine();
            }

            return jsonBuilder;
        }

        private static JsonSchemaHeader BuildCollectionValidator(SchemaObject schema, CollectionDifferences diffs)
        {
            var jsonSchema = new JsonSchema
            {
                BsonType = schema.BsonType,
                Title = $"{schema.Name} Object Validation",
                Properties = new Dictionary<string, JsonProperty>{
                    {
                        "Document", new JsonProperty
                        {
                            BsonType = "object",
                            Description = "Default document inside wrapper"
                        }
                    }
                },
                Required = ["Document"]
            };

            BuildProperties(diffs, schema.Properties, jsonSchema.Properties, "Document");
            return new JsonSchemaHeader { JsonSchema = jsonSchema};
        }

        private static void BuildRequired(IEnumerable<SchemaProperty> properties, JsonProperty jsonSchema)
        {
            var required = properties.Where(x => x.IsRequired && !string.IsNullOrWhiteSpace(x.QueryName)).Select(x => x.QueryName).ToList();

            if (required != null && required.Count > 0)
            {
                jsonSchema.Required ??= [];
                jsonSchema.Required.AddRange(required!);
            }
        }

        private static void BuildProperties(CollectionDifferences diffs, IEnumerable<SchemaProperty> properties, Dictionary<string, JsonProperty> jsonProperties, string key, HashSet<string>? recursionCheck = null, Stack<string>? path = null, int recursions = 0)
        {
            recursionCheck ??= [];
            path ??= [];

            if (recursions > 100)
            {
                AnsiConsole.WriteLine("Max number of recursions met, Fix the circlular dependencies to continue.");
                Environment.Exit(1);    
            }

            if (jsonProperties.TryGetValue(key, out var jsonProperty))
            {
                var schemaProperties = properties.Where(x => !string.IsNullOrWhiteSpace(x.QueryName));
                BuildRequired(schemaProperties, jsonProperty);
                foreach (var property in schemaProperties)
                {   
                    if (!recursionCheck!.Add(property.TypeName!))
                    {
                        throw new InvalidOperationException(
                            $"Circular MongoObject dependency detected: {string.Join(" -> ", recursionCheck)} -> {property.TypeName}");
                    }

                    jsonProperty.Properties ??= new();
                    jsonProperty.Properties[$"{property.QueryName!}"] = new JsonProperty
                    {
                        BsonType = property.BsonType,
                        Description = $"{property.QueryName} must be a ({property.BsonType}){(property.IsRequired ? " and is required." : ".")}"
                    };
                    
                    if (property.BsonType == "object")
                    {
                        List<SchemaObject> nested = diffs.ExistingCollections.Where(x => x.Value.TypeName == property.TypeName).Select(x => x.Value).ToList();
                        List<SchemaObject> newNested = diffs.ExistingCollections.Where(x => x.Value.TypeName == property.TypeName).Select(x => x.Value).ToList();
                        nested.AddRange(newNested);

                        if (nested.Count > 0)
                        {
                            BuildProperties(diffs, nested[0].Properties, jsonProperty.Properties, property.QueryName!, recursionCheck, path, recursions++);
                        }
                    }
                    recursionCheck.Remove(property.TypeName!);
                }
            }
        }
    }
}