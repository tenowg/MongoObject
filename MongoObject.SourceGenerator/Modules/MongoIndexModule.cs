using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;
using System.Collections.Immutable;
using System.Text;

namespace MongoObject.SourceGenerator.Modules
{
    internal class MongoIndexModule : ICodeModuleMultiple
    {

        public void Execute(SourceProductionContext context, (ImmutableArray<CommonModel?> models, string rootNamespace) args)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"namespace {args.rootNamespace};");
            sb.AppendLine();
            sb.AppendLine($"public static class {args.rootNamespace.Replace(".", "")}IndexExtensions");
            sb.AppendLine("{");
            
            foreach (var model in args.models)
            {
                if (model == null) continue;
                sb.AppendLine($"    public static void CreateIndex(this global::MongoObject.Core.Interfaces.IMongoConnection connection)");
                sb.AppendLine("    {");
                sb.AppendLine($"        if(connection is global::MongoObject.Core.Interfaces.IMongoConnection<global::{model.Namespace}.{model.Name}> col)");
                sb.AppendLine("        {");
                sb.AppendLine("            // Create indexes here");
                sb.AppendLine($"            var indexModels = new List<global::MongoDB.Driver.CreateIndexModel<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.Name}>>>();");
                
                bool? unique = false;
                string indexName = "";
                foreach (var index in model.Indexes)
                {
                    indexName = index.Name;
                    sb.Append($"             indexModels.Add(new(global::MongoDB.Driver.Builders<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.Name}>>.IndexKeys");
                    //sb.AppendLine($"{index.Name}");
                    foreach (var prop in index.Properties)
                    {        
                        foreach (var ind in prop.Indexes)
                        {
                            if (ind.IndexName == index.Name)
                            {

                                sb.Append($"\n                 .{ind.Order}(x => x.Document.{prop.Name})");
                                unique = ind.Unique;
                            }

                        }
                    }
                    sb.AppendLine($",\n                 new global::MongoDB.Driver.CreateIndexOptions {{ Name = \"{indexName}\", Unique = {unique?.ToString().ToLowerInvariant()} }}));");
                    
                }
                sb.AppendLine("            col.Collection.Indexes.CreateMany(indexModels);");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine("}");
            }

            context.AddSource($"{args.rootNamespace.Replace(".", "_")}_IndexBuilder.g.cs", sb.ToString());
        }
    }
}
