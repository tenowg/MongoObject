using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;
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
            sb.AppendLine($"namespace {model.Namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public record {projectionTypeName} : global::MongoObject.Core.Interfaces.IProjectionBase, global::MongoObject.Core.Interfaces.IProjectionBase<global::{model.Namespace}.{model.Name}, global::{model.Namespace}.{projectionTypeName}>");
            sb.AppendLine("    {");

            // Properties - only for Include properties
            foreach (var property in projection.Properties)
            {
                if (!string.IsNullOrEmpty(property.EnumName) && property.EnumName == "Include")
                {
                    sb.AppendLine($"        public {property.FullName}? {property.Name} {{ get; set; }}");
                }
            }

            sb.AppendLine();

            // Public ToMongoProjection method returning concrete type
            // Uses Expression which defines both the fields and the result type
            sb.AppendLine($"        public global::MongoDB.Driver.ProjectionDefinition<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.Name}>, global::{model.Namespace}.{projectionTypeName}> ToMongoProjection(string prefix = \"\")");
            sb.AppendLine("        {");
            sb.AppendLine($"            var builder = global::MongoDB.Driver.Builders<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.Name}>>.Projection;");
            sb.AppendLine();
            sb.AppendLine($"            return builder.Expression(u => new global::{model.Namespace}.{projectionTypeName}");
            sb.AppendLine("            {");

            foreach (var property in projection.Properties)
            {
                if (!string.IsNullOrEmpty(property.EnumName) && property.EnumName == "Include")
                {
                    sb.AppendLine($"                {property.Name} = u.Document!.{property.Name},");
                }
            }

            sb.AppendLine("            });");
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
            sb.AppendLine($"            return new global::MongoDB.Driver.BsonDocumentProjectionDefinition<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.Name}>, global::{model.Namespace}.{projectionTypeName}>(rendered.Document);");
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource($"{model.Namespace.Replace(".", "_")}_{model.Name}.{projection.Name}.g.cs", sb.ToString());
        }
    }
}
