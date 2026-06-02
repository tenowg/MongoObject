using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;
using System.Text;

namespace MongoObject.SourceGenerator.Modules
{
    internal class MetadataModule : ICodeModule
    {
        public void Execute(SourceProductionContext context, (CommonModel model, Compilation comp) provider)
        {
            var model = provider.model;

            // Generate MetaQuery class
            GenerateMetaQuery(context, model);

            // Generate MetaRecord class
            GenerateMetaRecord(context, model);
        }

        private static void GenerateMetaQuery(SourceProductionContext context, CommonModel model)
        {
            var sb = new StringBuilder(4096);

            sb.AppendLine("#nullable enable");
            sb.AppendLine("// auto-generated");
            sb.AppendLine($"namespace {model.Namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public partial record {model.Metadata.Name}Query : global::MongoObject.Core.Interfaces.MetadataSearch, global::MongoObject.Core.Interfaces.IMetadataSearchBase");
            sb.AppendLine("    {");
            sb.AppendLine("        public global::MongoObject.Core.Data.QueryVal<System.DateTime>? CreatedAt { get; set; }");
            sb.AppendLine("        public global::MongoObject.Core.Data.QueryVal<System.DateTime>? LastModifiedAt { get; set; }");
            sb.AppendLine("        public global::MongoObject.Core.Data.QueryVal<int>? Version { get; set; }");

            foreach (var prop in model.Metadata.Properties)
            {
                sb.AppendLine($"        public global::MongoObject.Core.Data.QueryVal<{prop.FullName}>? {prop.Name} {{ get; set; }}");
            }

            sb.AppendLine();
            sb.AppendLine("        public global::MongoDB.Driver.FilterDefinition<global::MongoObject.Core.Data.MongoDocument<T>> ToMongoFilter<T>() where T : class, global::MongoObject.Core.Interfaces.IDocumentFile, new()");
            sb.AppendLine("        {");
            sb.AppendLine("            var builder = global::MongoDB.Driver.Builders<global::MongoObject.Core.Data.MongoDocument<T>>.Filter;");
            sb.AppendLine("            var filters = new System.Collections.Generic.List<global::MongoDB.Driver.FilterDefinition<global::MongoObject.Core.Data.MongoDocument<T>>>();");
            sb.AppendLine();
            sb.AppendLine("            if (CreatedAt != null)");
            sb.AppendLine("            {");
            sb.AppendLine("                filters.Add(CreateFilter(builder, \"Metadata.CreatedAt\", CreatedAt));");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            if (LastModifiedAt != null)");
            sb.AppendLine("            {");
            sb.AppendLine("                filters.Add(CreateFilter(builder, \"Metadata.LastModifiedAt\", LastModifiedAt));");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            if (Version != null)");
            sb.AppendLine("            {");
            sb.AppendLine("                filters.Add(CreateFilter(builder, \"Metadata.Version\", Version));");
            sb.AppendLine("            }");

            foreach (var prop in model.Metadata.Properties)
            {
                sb.AppendLine();
                sb.AppendLine($"            if ({prop.Name} != null)");
                sb.AppendLine("            {");
                sb.AppendLine($"                filters.Add(CreateFilter(builder, \"Metadata.{prop.Name}\", {prop.Name}));");
                sb.AppendLine("            }");
            }

            sb.AppendLine();
            sb.AppendLine("            return filters.Count == 0 ? builder.Empty : builder.And(filters);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource($"{model.Namespace.Replace(".", "_")}_{model.Name}.MetaQuery.g.cs", sb.ToString());
        }

        private static void GenerateMetaRecord(SourceProductionContext context, CommonModel model)
        {
            var sb = new StringBuilder(2048);

            sb.AppendLine("#nullable enable");
            sb.AppendLine("// auto-generated");
            sb.AppendLine($"namespace {model.Namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public partial record {model.Metadata.Name}Record : global::MongoObject.Core.Interfaces.IMetadataBase");
            sb.AppendLine("    {");
            sb.AppendLine("        public System.DateTime? CreatedAt { get; set; }");
            sb.AppendLine("        public System.DateTime? LastModifiedAt { get; set; }");
            sb.AppendLine("        public int? Version { get; set; }");

            foreach (var prop in model.Metadata.Properties)
            {
                // Handle nullable types - strip the "?" from FullName if present, then add our own
                var propType = prop.FullName;
                if (propType.EndsWith("?"))
                {
                    propType = propType.Substring(0, propType.Length - 1);
                }
                sb.AppendLine($"        public {propType}? {prop.Name} {{ get; set; }}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource($"{model.Namespace.Replace(".", "_")}_{model.Name}.Meta.g.cs", sb.ToString());
        }
    }
}
