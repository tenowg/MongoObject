using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;
using System.Linq;
using System.Text;

namespace MongoObject.SourceGenerator.Modules
{
    internal class ProjectionModule : ICodeModule
    {
        public void Execute(SourceProductionContext context, (CommonModel model, Compilation comp) provider)
        {
            var model = provider.model;
            var ct = context.CancellationToken;

            foreach (var projection in model.Projections)
            {
                ct.ThrowIfCancellationRequested();
                GenerateProjection(context, model, projection);
            }
        }

        private static void GenerateProjection(SourceProductionContext context, CommonModel model, ProjectionModel projection)
        {
            var sb = new StringBuilder(4096);
            var projectionTypeName = $"{model.Name}{projection.Name}";

            sb.AppendLine("#nullable enable");
            sb.AppendLine("// auto-generated");
            sb.AppendLine("using MongoDB.Driver;");
            sb.AppendLine();
            sb.AppendLine($"namespace {model.Namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public record {projectionTypeName} : global::MongoObject.Core.Interfaces.IProjectionBase, global::MongoObject.Core.Interfaces.IProjectionBase<global::{model.Namespace}.{model.Name}, global::{model.Namespace}.{projectionTypeName}>");
            sb.AppendLine("    {");

            // Properties - only for Include properties
            foreach (var property in projection.Properties)
            {
                if (!string.IsNullOrEmpty(property.EnumName) && (property.EnumName == "Include" || property.EnumName == "Vector" || property.EnumName == "AutoVector"))
                {
                    sb.AppendLine($"        public {property.FullName}? {property.Name} {{ get; set; }}");
                }

                if (!string.IsNullOrEmpty(property.EnumName) && property.EnumName == "Slice")
                {
                    sb.AppendLine($"        public {property.FullName} {property.Name} {{ get; set; }} = new {property.FullName}();");
                }
            }

            if (projection.Properties.Any(p => p.EnumName == "Vector" || p.EnumName == "AutoVector"))
            {
                sb.AppendLine("        public float Score { get; set; }");
            }

            sb.AppendLine($"        private Dictionary<string, global::MongoObject.Core.Data.ProjectionVal.Slice> _sliceProjections = new Dictionary<string, global::MongoObject.Core.Data.ProjectionVal.Slice>();");
            sb.AppendLine();

            sb.AppendLine($"        public void SetSliceProjection(string propertyName, global::MongoObject.Core.Data.ProjectionVal.Slice slice)");
            sb.AppendLine("        {");
            sb.AppendLine("            _sliceProjections[propertyName] = slice;");
            sb.AppendLine("        }");

            // Public ToMongoProjection method returning concrete type
            // Uses Expression which defines both the fields and the result type
            sb.AppendLine($"        public global::MongoDB.Driver.ProjectionDefinition<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.Name}>, global::{model.Namespace}.{projectionTypeName}> ToMongoProjection(string prefix = \"\")");
            sb.AppendLine("        {");
            sb.AppendLine($"            var projectionDoc = new global::MongoDB.Bson.BsonDocument();");
            sb.AppendLine();
            sb.AppendLine($"            projectionDoc[\"_id\"] = 0;");
            foreach (var property in projection.Properties)
            {
                if (!string.IsNullOrEmpty(property.EnumName))
                {
                    
                    if (property.EnumName == "Include" || property.EnumName == "AutoVector")
                    {
                        sb.AppendLine($"            projectionDoc[\"{property.Name}\"] = \"$Document.{property.QueryName}\";");
                    }
                    else if (property.EnumName == "Slice")
                    {
                        sb.AppendLine($"            if (_sliceProjections.TryGetValue(\"{property.Name}\", out var slice))");
                        sb.AppendLine($"            {{");
                        sb.AppendLine($"                projectionDoc[\"Document.{property.Name}\"] = new global::MongoDB.Bson.BsonDocument(\"$slice\", new global::MongoDB.Bson.BsonArray");
                        sb.AppendLine($"                {{");
                        sb.AppendLine($"                    slice.Skip,");
                        sb.AppendLine($"                    slice.Limit");
                        sb.AppendLine($"                }});");
                        sb.AppendLine($"            }}");
                    }
                }
            }

            if (projection.Properties.Any(p => p.EnumName == "Vector" || p.EnumName == "AutoVector"))
            {
                sb.AppendLine("             projectionDoc[\"Score\"] = new global::MongoDB.Bson.BsonDocument(\"$meta\", \"vectorSearchScore\");");
            }

            sb.AppendLine("            return new BsonDocumentProjectionDefinition<");
            sb.AppendLine($"                global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.Name}>,");
            sb.AppendLine($"                global::{model.Namespace}.{projectionTypeName}>(projectionDoc, new {model.Name}{projection.Name}Serializer());");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Explicit interface implementation returning IProjectionBase<T>
            // Uses BsonDocument as intermediate to convert between projection types
            sb.AppendLine($"        global::MongoDB.Driver.ProjectionDefinition<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.Name}>, global::{model.Namespace}.{projectionTypeName}> global::MongoObject.Core.Interfaces.IProjectionBase<global::{model.Namespace}.{model.Name}, global::{model.Namespace}.{projectionTypeName}>.ToMongoProjection(string prefix)");
            sb.AppendLine("        {");
            sb.AppendLine("            var concreteProjection = ToMongoProjection(prefix);");
            sb.AppendLine($"            var serializer = global::MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry.GetSerializer<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.Name}>>();");
            sb.AppendLine($"            var renderArgs = new global::MongoDB.Driver.RenderArgs<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.Name}>>(serializer, global::MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry);");
            sb.AppendLine("            var rendered = concreteProjection.Render(renderArgs);");
            sb.AppendLine($"            return new global::MongoDB.Driver.BsonDocumentProjectionDefinition<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.Name}>, global::{model.Namespace}.{projectionTypeName}>(rendered.Document, new {model.Name}{projection.Name}Serializer());");
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource($"{model.Namespace.Replace(".", "_")}_{model.Name}.{projection.Name}.g.cs", sb.ToString());
        }
    }
}
