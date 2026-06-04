using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;
using System.Text;

namespace MongoObject.SourceGenerator.Modules
{
    internal class DocumentSearchModule : CodeModule
    {
        public override void Execute(SourceProductionContext context, (CommonModel model, Compilation comp) provider)
        {
            var model = provider.model;
            var sb = new StringBuilder(4096);

            sb.AppendLine("#nullable enable");
            sb.AppendLine("// auto-generated");
            sb.AppendLine($"namespace {model.Namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public record {model.Name}Query : global::MongoObject.Core.Interfaces.MetadataSearch, global::MongoObject.Core.Interfaces.IClassSearch, global::MongoObject.Core.Interfaces.IClassSearch<{model.Name}>");
            sb.AppendLine("    {");

            // Generate query properties
            foreach (var prop in model.Properties)
            {
                if (prop.IsMongoObject)
                {
                    // Use TypeName (the property's type name) for the query type, not the property name
                    sb.AppendLine($"        public global::{model.Namespace}.{prop.TypeName}Query? {prop.Name} {{ get; set; }}");
                }
                else
                {
                    sb.AppendLine($"        public global::MongoObject.Core.Data.QueryVal<{prop.FullName}>? {prop.Name} {{ get; set; }}");
                }
            }
            sb.AppendLine($"        public Action<global::{model.Namespace}.{model.Name}Query>[]? Or {{ get; set; }}");
            sb.AppendLine($"        public Action<global::{model.Namespace}.{model.Name}Query>[]? And {{ get; set; }}");

            // Generate ToMongoFilter method
            sb.AppendLine();
            sb.AppendLine($"        public global::MongoDB.Driver.FilterDefinition<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.Name}>> ToMongoFilter(string prefix = \"\")");
            sb.AppendLine("        {");
            sb.AppendLine($"            var builder = global::MongoDB.Driver.Builders<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.Name}>>.Filter;");
            sb.AppendLine($"            var filters = new System.Collections.Generic.List<global::MongoDB.Driver.FilterDefinition<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.Name}>>>();");

            foreach (var prop in model.Properties)
            {
                sb.AppendLine();
                sb.AppendLine($"            if ({prop.Name} != null)");
                sb.AppendLine("            {");

                if (prop.IsMongoObject)
                {
                    // Use BsonDocument intermediate to convert nested filter type to parent filter type
                    sb.AppendLine($"                var nestedFilter_{prop.Name} = {prop.Name}.ToMongoFilter($\"{{(string.IsNullOrEmpty(prefix) ? \"\" : prefix + \".\")}}Document.{prop.Name}\");");
                    sb.AppendLine($"                var nestedSerializer_{prop.Name} = global::MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry.GetSerializer<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{prop.TypeName}>>();");
                    sb.AppendLine($"                var renderArgs_{prop.Name} = new global::MongoDB.Driver.RenderArgs<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{prop.TypeName}>>(nestedSerializer_{prop.Name}, global::MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry);");
                    sb.AppendLine($"                var bsonDoc_{prop.Name} = nestedFilter_{prop.Name}.Render(renderArgs_{prop.Name});");
                    sb.AppendLine($"                filters.Add(new global::MongoDB.Driver.BsonDocumentFilterDefinition<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.Name}>>(bsonDoc_{prop.Name}));");
                }
                else
                {
                    sb.AppendLine($"                filters.Add(CreateFilter(builder, \"Document.{prop.Name}\", {prop.Name}));");
                }

                sb.AppendLine("            }");
            }

            sb.AppendLine($"            if (Or is {{ Length: > 0 }})");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                var orFilters = Or.Select(o => {{");
            sb.AppendLine($"                    var query = new {model.Namespace}.{model.Name}Query();");
            sb.AppendLine($"                    o.Invoke(query);");
            sb.AppendLine($"                    return query.ToMongoFilter(prefix);");
            sb.AppendLine($"                }});");
            sb.AppendLine($"                filters.Add(builder.Or(orFilters));");
            sb.AppendLine($"            }}");

            sb.AppendLine($"            if (And is {{ Length: > 0 }})");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                var andFilters = And.Select(o => {{");
            sb.AppendLine($"                    var query = new {model.Namespace}.{model.Name}Query();");
            sb.AppendLine($"                    o.Invoke(query);");
            sb.AppendLine($"                    return query.ToMongoFilter(prefix);");
            sb.AppendLine($"                }});");
            sb.AppendLine($"                filters.Add(builder.And(andFilters));");
            sb.AppendLine($"            }}");

            sb.AppendLine();
            sb.AppendLine("            return filters.Count == 0 ? builder.Empty : builder.And(filters);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource($"{model.Namespace.Replace(".", "_")}_{model.Name}.Query.g.cs", sb.ToString());
        }
    }
}
