using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MongoObject.SourceGenerator.Helpers;
using MongoObject.SourceGenerator.Models;

namespace MongoObject.SourceGenerator.Generators
{
    [Generator]
    internal partial class IndexGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider = context.SyntaxProvider.ForAttributeWithMetadataName("MongoObject.Core.Attributes.MongoIndexAttribute",
                predicate: static (s, _) => s is ClassDeclarationSyntax,
                transform: (ctx, ct) => BuildIndexModel(ctx, ct))
            .Where(static m => m is not null);

            var values = context.AnalyzerConfigOptionsProvider.Select(static (options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
                return rootNamespace ?? "DefaultName";
            });

            var combinedProvider = provider.Collect().Combine(values);
            context.RegisterSourceOutput(combinedProvider, BuildIndexBson);
        }

        private IndexModelProvider? BuildIndexModel(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var compilation = ctx.SemanticModel.Compilation;
            var mongoObjectAttrSymbol = compilation.GetTypeByMetadataName("MongoObject.Core.Attributes.MongoObjectAttribute");
            var mongoIndexAttrSymbol = compilation.GetTypeByMetadataName("MongoObject.Core.Attributes.MongoIndexAttribute");
            var fieldIndexAttrSymbol = compilation.GetTypeByMetadataName("MongoObject.Core.Attributes.FieldIndexAttribute");
            var bsonElementAttrSymbol = compilation.GetTypeByMetadataName("MongoDB.Bson.Serialization.Attributes.BsonElementAttribute");

            var classSymbol = (INamedTypeSymbol)ctx.TargetSymbol;

            var mongoAttr = classSymbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, mongoObjectAttrSymbol));

            if (mongoAttr == null)
                return null; 

            var databaseName = mongoAttr.NamedArguments.FirstOrDefault(n => n.Key == "DatabaseName").Value.Value?.ToString();
            var collectionName = mongoAttr.NamedArguments.FirstOrDefault(n => n.Key == "CollectionName").Value.Value?.ToString();

            var fields = classSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .SelectMany(prop => prop.GetAttributes()
                .Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, fieldIndexAttrSymbol))
                .Select(a => {
                    var isBsonElement = bsonElementAttrSymbol != null && prop.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, bsonElementAttrSymbol));
                    return new IndexProperty {
                        PropertyName = prop.Name,
                        IndexName = (string)a.ConstructorArguments[0].Value!,
                        Direction = GetIndexTypeName(a),
                        QueryName = isBsonElement ? prop.GetAttributes().First(b => SymbolEqualityComparer.Default.Equals(b.AttributeClass, bsonElementAttrSymbol)).ConstructorArguments.FirstOrDefault().Value as string ?? prop.Name : prop.Name,
                    };
                    })
                )
                .ToImmutableArray();

            var indexes = classSymbol.GetAttributes()
                .Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, mongoIndexAttrSymbol))
                .Select(a => new IndexModel {
                    DatabaseName = databaseName ?? string.Empty,
                    CollectionName = collectionName ?? string.Empty,
                    Name = (string)a.ConstructorArguments[0].Value!,
                    Properties = fields.Where(f => f.IndexName == (string)a.ConstructorArguments[0].Value!).ToImmutableArray(),
                    IsUnique = a.NamedArguments.FirstOrDefault(n => n.Key == "Unique").Value.Value is true
                })
                .ToImmutableArray();

            return new IndexModelProvider { indexModels = indexes, CollectionName = collectionName!, DatabaseName = databaseName! };
        }

        private void BuildIndexBson(SourceProductionContext context, (ImmutableArray<IndexModelProvider?> Left, string Right) tuple)
        {
            var models = tuple.Left;
            if (models.Length == 0) return;
            var rootNamespace = tuple.Right;
            var sb = new IndentedStringBuilder();
            var prefix = "Document";

            sb.AppendLine("// auto-generated");
            sb.AppendLine($"namespace {rootNamespace}.Extensions");
            using (sb.Block())
            {
                sb.AppendLine("internal static class IndexBuilder");
                using (sb.Block())
                {
                    sb.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
                    sb.AppendLine("public static void Initialize()");
                    using (sb.Block())
                    {
                        sb.AppendLine("var indexDoc = new global::MongoDB.Bson.BsonDocument();");
                        sb.AppendLine("if (global::MongoObject.Core.Extensions.MongoObjectsPluginRegistry.SchemaDocument.TryGetValue(\"indexes\", out var indexes))");
                        using (sb.Block())
                        {
                            sb.AppendLine("indexDoc = indexes.AsBsonDocument;");
                        }
                        foreach (var provider in models)
                        {
                            if (provider == null)
                            {
                                sb.AppendLine("// provider is null, skipping");
                                continue;
                            }
                            if (provider.indexModels.Length > 0)
                            {
                                sb.AppendLine($"indexDoc.Add(\"{provider.DatabaseName}.{provider.CollectionName}\", new global::MongoDB.Bson.BsonArray");

                                using (sb.Block())
                                {
                                    foreach (var model in provider.indexModels)
                                    { 
                                        sb.AppendLine($"new global::MongoDB.Bson.BsonDocument");
                                        using (sb.Block(closer: ","))
                                        {
                                            sb.AppendLine($"{{\"index_name\", \"{model.Name}\"}},");
                                            sb.AppendLine($"{{\"unique\", {model.IsUnique.ToString().ToLower()}}},");

                                            sb.AppendLine($"{{\"entities\", new global::MongoDB.Bson.BsonDocument");
                                            using (sb.Block())
                                            {
                                                foreach (var pr in model.Properties)
                                                {
                                                    sb.AppendLine($"{{\"{prefix}.{pr.QueryName}\", \"{pr.Direction}\"}},");
                                                }
                                            }
                                            sb.AppendLine("}");
                                        }
                                    }
                                }
                                sb.AppendLine(");");
                            }
                            else
                            {
                                sb.AppendLine("// No index models found for this provider");
                            }
                        }
                        sb.AppendLine($"//{models.Length} models processed");
                        sb.AppendLine($"global::MongoObject.Core.Extensions.MongoObjectsPluginRegistry.SchemaDocument[\"indexes\"] = indexDoc;");
                    }
                }
            }
            context.AddSource($"{rootNamespace.Replace(".", "_")}_IndexBuilder.g.cs", sb.ToString());
        }

        public static string GetIndexTypeName(AttributeData attributeData)
        {
            var projectionType = attributeData.NamedArguments.Where(x => x.Key == "Type").FirstOrDefault().Value.Value as int? ?? 0;
            return projectionType switch
            {
                0 => "Ascending",
                1 => "Descending",
                _ => throw new ArgumentOutOfRangeException(nameof(projectionType), $"Unsupported projection type: {projectionType}")
            };
        }
    }
}