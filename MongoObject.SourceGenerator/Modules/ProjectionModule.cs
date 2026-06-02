using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.SourceGenerator.Modules
{
    internal class ProjectionModule : ICodeModule
    {
        public void Execute(SourceProductionContext context, (CommonModel model, Compilation comp) provider)
        {
            (CommonModel model, Compilation comp) = provider;

            foreach (var projection in model.Projections)
            {
                string source = $@"
namespace {model.Namespace}
{{
    public record {model.Name}{projection.Name} : global::MongoObject.Core.Interfaces.IProjectionBase, global::MongoObject.Core.Interfaces.IProjectionBase<global::{model.Namespace}.{model.Name}>
    {{
";
                foreach (var property in projection.Properties)
                {
                    if (!string.IsNullOrEmpty(property.EnumName) && property.EnumName != "Exclude")
                    {
                        source += $"        public {property.FullName}? {property.Name} {{ get; set; }}\n";
                    }
                }

                source += $@"
        public global::MongoDB.Driver.ProjectionDefinition<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.Name}>> ToMongoProjection(string prefix = """")
        {{
            var projections = new List<global::MongoDB.Driver.ProjectionDefinition<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.Name}>>>();
            var builder = global::MongoDB.Driver.Builders<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.Name}>>.Projection;
";
                foreach (var property in projection.Properties)
                {
                    if (!string.IsNullOrEmpty(property.EnumName) && property.EnumName != "Exclude")
                    {
                        source += $"            projections.Add(builder.{property.EnumName}(x => x.Document.{property.Name}));\n";
                    }
                }
                source += $@"
            
            var expression = builder.Expression(u => new {model.Name}{projection.Name} 
            {{
";
                foreach (var property in projection.Properties)
                {
                    if (!string.IsNullOrEmpty(property.EnumName) && property.EnumName == "Include")
                    {
                        source += $"                {property.Name} = u.Document.{property.Name},";
                    }
                }

                source += $@"
            }});
            projections.Insert(0, expression);
            
            return builder.Combine(projections);
        }}
    }}
}}
";

                context.AddSource($"{model.Namespace.Replace(".", "_")}_{model.Name}.{projection.Name}.g.cs", source);
            }
        }
    }
}
