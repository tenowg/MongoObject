using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MongoObject.SourceGenerator.Helpers;
using MongoObject.SourceGenerator.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace MongoObject.SourceGenerator.Generators
{
    [Generator]
    internal class ProjectionGenerator : IIncrementalGenerator
    {
        private SymbolDisplayFormat format = SymbolDisplayFormat.FullyQualifiedFormat
            .WithMemberOptions(
                SymbolDisplayMemberOptions.IncludeContainingType |
                SymbolDisplayMemberOptions.IncludeType
            )
            .WithMiscellaneousOptions(
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
            );

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //Debugger.Launch();
            var provider = context.SyntaxProvider.ForAttributeWithMetadataName("MongoObject.Core.Attributes.MongoObjectAttribute",
                predicate: static (s, _) => s is ClassDeclarationSyntax,
                transform: (ctx, ct) => BuildProjectionModel(ctx, ct))
            .Where(static m => m is not null);

            var values = context.AnalyzerConfigOptionsProvider.Select(static (options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
                return rootNamespace ?? "DefaultName";
            });

            var combinedProvider = provider.Collect().Combine(values);
            context.RegisterSourceOutput(combinedProvider, BuildProjectionBson);
        }

        private void BuildProjectionBson(SourceProductionContext context, (ImmutableArray<ImmutableArray<ProjectionModel>?> Left, string Right) tuple)
        {
            var projections = tuple.Left;

            foreach (var projection in projections)
            {
                foreach (var model in projection!)
                {
                    var sb = new IndentedStringBuilder();
                    var projectionTypeName = $"{model.ModelName}{model.Name}";

                    sb.AppendLine("#nullable enable");
                    sb.AppendLine("// auto-generated");
                    sb.AppendLine("using MongoDB.Driver;");
                    sb.AppendLine();
                    sb.AppendLine($"namespace {model.Namespace}");
                    using (sb.Block())
                    {
                        sb.AppendLine($"public record {projectionTypeName} : global::MongoObject.Core.Interfaces.IProjectionBase, global::MongoObject.Core.Interfaces.IProjectionBase<global::{model.Namespace}.{model.ModelName}, global::{model.Namespace}.{projectionTypeName}>");
                        using (sb.Block())
                        {
                            // Properties - only for Include properties
                            foreach (var property in model.Properties)
                            {
                                if (!string.IsNullOrEmpty(property.EnumName) && (property.EnumName == "Include" || property.EnumName == "Vector" || property.EnumName == "AutoVector"))
                                {
                                    sb.AppendLine($"public {property.FullName}? {property.Name} {{ get; set; }}");
                                }

                                if (!string.IsNullOrEmpty(property.EnumName) && property.EnumName == "Slice")
                                {
                                    sb.AppendLine($"public {property.FullName} {property.Name} {{ get; set; }} = new {property.FullName}();");
                                }
                            }

                            if (model.Properties.Any(p => p.EnumName == "Vector" || p.EnumName == "AutoVector"))
                            {
                                sb.AppendLine("public float Score { get; set; }");
                            }

                            sb.AppendLine($"private Dictionary<string, global::MongoObject.Core.Data.ProjectionVal.Slice> _sliceProjections = new Dictionary<string, global::MongoObject.Core.Data.ProjectionVal.Slice>();");
                            sb.AppendLine();

                            sb.AppendLine($"public void SetSliceProjection(string propertyName, global::MongoObject.Core.Data.ProjectionVal.Slice slice)");
                            using (sb.Block())
                            {
                                sb.AppendLine("_sliceProjections[propertyName] = slice;");
                            }

                            // Public ToMongoProjection method returning concrete type
                            // Uses Expression which defines both the fields and the result type
                            sb.AppendLine($"public global::MongoDB.Driver.ProjectionDefinition<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.ModelName}>, global::{model.Namespace}.{projectionTypeName}> ToMongoProjection(string prefix = \"\")");
                            using (sb.Block())
                            {
                                sb.AppendLine($"var projectionDoc = new global::MongoDB.Bson.BsonDocument();");
                                sb.AppendLine();
                                sb.AppendLine($"projectionDoc[\"_id\"] = 0;");
                                foreach (var property in model.Properties)
                                {
                                    if (!string.IsNullOrEmpty(property.EnumName))
                                    {
                                        if (property.EnumName == "Include" || property.EnumName == "AutoVector")
                                        {
                                            sb.AppendLine($"projectionDoc[\"{property.QueryName}\"] = \"$Document.{property.QueryName}\";");
                                        }
                                        else if (property.EnumName == "Slice")
                                        {
                                            sb.AppendLine($"if (_sliceProjections.TryGetValue(\"{property.Name}\", out var slice))");
                                            using (sb.Block())
                                            {
                                                sb.AppendLine($"projectionDoc[\"Document.{property.Name}\"] = new global::MongoDB.Bson.BsonDocument(\"$slice\", new global::MongoDB.Bson.BsonArray");
                                                using (sb.Block(closer: ");"))
                                                {
                                                    sb.AppendLine($"slice.Skip,");
                                                    sb.AppendLine($"slice.Limit");
                                                }
                                            }
                                        }
                                    }
                                }

                                if (model.Properties.Any(p => p.EnumName == "Vector" || p.EnumName == "AutoVector"))
                                {
                                    sb.AppendLine("projectionDoc[\"Score\"] = new global::MongoDB.Bson.BsonDocument(\"$meta\", \"vectorSearchScore\");");
                                }

                                sb.AppendLine("return new BsonDocumentProjectionDefinition<");
                                sb.AppendLine($"    global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.ModelName}>,");
                                sb.AppendLine($"    global::{model.Namespace}.{projectionTypeName}>(projectionDoc, new {model.ModelName}{model.Name}Serializer());");
                            }
                            sb.AppendLine();

                            // Explicit interface implementation returning IProjectionBase<T>
                            // Uses BsonDocument as intermediate to convert between projection types
                            sb.AppendLine($"global::MongoDB.Driver.ProjectionDefinition<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.ModelName}>, global::{model.Namespace}.{projectionTypeName}> global::MongoObject.Core.Interfaces.IProjectionBase<global::{model.Namespace}.{model.ModelName}, global::{model.Namespace}.{projectionTypeName}>.ToMongoProjection(string prefix)");
                            using (sb.Block())
                            {
                                sb.AppendLine("var concreteProjection = ToMongoProjection(prefix);");
                                sb.AppendLine($"var serializer = global::MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry.GetSerializer<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.ModelName}>>();");
                                sb.AppendLine($"var renderArgs = new global::MongoDB.Driver.RenderArgs<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.ModelName}>>(serializer, global::MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry);");
                                sb.AppendLine("var rendered = concreteProjection.Render(renderArgs);");
                                sb.AppendLine($"return new global::MongoDB.Driver.BsonDocumentProjectionDefinition<global::MongoObject.Core.Data.MongoDocument<global::{model.Namespace}.{model.ModelName}>, global::{model.Namespace}.{projectionTypeName}>(rendered.Document, new {model.ModelName}{model.Name}Serializer());");
                            }
                        }    
                    }
                    context.AddSource($"{model.Namespace.Replace(".", "_")}_{model.ModelName}.{model.Name}.g.cs", sb.ToString());
                }
                BuildBsonSerializers(context, projection);
            }
            
        }

        public void BuildBsonSerializers(SourceProductionContext context, ImmutableArray<ProjectionModel>? projections)
        {
            //var model = provider.model;
            foreach (var model in projections)
            {
                var sb = new IndentedStringBuilder();
                sb.AppendLine("using MongoDB.Bson.IO;");
                sb.AppendLine();
                sb.AppendLine($"public class {model.ModelName}{model.Name}Serializer : global::MongoDB.Bson.Serialization.Serializers.SerializerBase<global::{model.Namespace}.{model.ModelName}{model.Name}>");
                using (sb.Block())
                {
                    sb.AppendLine($"public override void Serialize(");
                    sb.AppendLine($"    global::MongoDB.Bson.Serialization.BsonSerializationContext context,");
                    sb.AppendLine($"    global::MongoDB.Bson.Serialization.BsonSerializationArgs args,");
                    sb.AppendLine($"    global::{model.Namespace}.{model.ModelName}{model.Name} value)");
                    using (sb.Block())
                    {
                        sb.AppendLine("var bsonWriter = context.Writer;");
                        sb.AppendLine("bsonWriter.WriteStartDocument();");
                        sb.AppendLine("// Implementation for serialization");
                        sb.AppendLine("bsonWriter.WriteEndDocument();");
                    }
                    sb.AppendLine();
                    sb.AppendLine($"public override {model.Namespace}.{model.ModelName}{model.Name} Deserialize(global::MongoDB.Bson.Serialization.BsonDeserializationContext context, global::MongoDB.Bson.Serialization.BsonDeserializationArgs args)");
                    using (sb.Block())
                    {
                        sb.AppendLine("var bsonReader = context.Reader;");
                        sb.AppendLine("bsonReader.ReadStartDocument();");
                        sb.AppendLine();
                        sb.AppendLine($"var result = new global::{model.Namespace}.{model.ModelName}{model.Name}();");
                        sb.AppendLine();
                        // Loop through the fields returned by MongoDB
                        sb.AppendLine($"while (bsonReader.ReadBsonType() != global::MongoDB.Bson.BsonType.EndOfDocument)");
                        using (sb.Block())
                        {
                            sb.AppendLine($"var name = bsonReader.ReadName();");
                            sb.AppendLine("if (name == \"Document\") // Handle nested properties safely");
                            using (sb.Block())
                            {
                                sb.AppendLine("bsonReader.ReadStartDocument();");
                                sb.AppendLine("while (bsonReader.ReadBsonType() != global::MongoDB.Bson.BsonType.EndOfDocument)");
                                using (sb.Block())
                                {
                                    sb.AppendLine("var subName = bsonReader.ReadName();");
                                    foreach (var prop in model.Properties)
                                    {
                                        if (prop.IsBsonIgnore) continue; // Skip ignored properties
                                        if (prop.EnumName == "Slice")
                                        {
                                            sb.AppendLine($"if (subName == \"{prop.QueryName}\")");
                                            using (sb.Block())
                                            {
                                                sb.AppendLine("// Deserialize the list directly into your flat property");
                                                sb.AppendLine($"result.{prop.Name} = global::MongoDB.Bson.Serialization.BsonSerializer");
                                                sb.AppendLine($"    .Deserialize<{prop.FullName}>(bsonReader);");
                                                sb.AppendLine("continue;");
                                            }
                                        }
                                        else if (!prop.IsBsonIgnore && prop.EnumName != "Exclude")
                                        {
                                            sb.AppendLine($"if (subName == \"{prop.QueryName}\")");
                                            using (sb.Block())
                                            {
                                                sb.AppendLine("// Deserialize the value directly into the property");
                                                sb.AppendLine($"result.{prop.Name} = global::MongoDB.Bson.Serialization.BsonSerializer.Deserialize<{prop.FullName}>(bsonReader);");
                                                sb.AppendLine("continue;");
                                            }
                                        }
                                    }
                                    sb.AppendLine("bsonReader.SkipValue();");
                                }
                                sb.AppendLine("bsonReader.ReadEndDocument();");
                            }

                            foreach (var prop in model.Properties)
                            {
                                if (prop.IsBsonIgnore) continue; // Skip ignored properties
                                if (prop.EnumName == "Slice")
                                {
                                    sb.AppendLine($"if (name == \"{prop.QueryName}\")");
                                    using (sb.Block())
                                    {
                                        sb.AppendLine("// Deserialize the list directly into your flat property");
                                        sb.AppendLine($"result.{prop.Name} = global::MongoDB.Bson.Serialization.BsonSerializer");
                                        sb.AppendLine($"    .Deserialize<{prop.FullName}>(bsonReader);");
                                        sb.AppendLine("continue;");
                                    }
                                }
                                else if (!prop.IsBsonIgnore && prop.EnumName != "Exclude")
                                {
                                    sb.AppendLine($"if (name == \"{prop.QueryName}\")");
                                    using (sb.Block())
                                    {
                                        sb.AppendLine("// Deserialize the value directly into the property");
                                        sb.AppendLine($"result.{prop.Name} = global::MongoDB.Bson.Serialization.BsonSerializer.Deserialize<{prop.FullName}>(bsonReader);");
                                        sb.AppendLine("continue;");
                                    }
                                }

                                if (!prop.IsBsonIgnore && (prop.EnumName == "Vector" || prop.EnumName == "AutoVector"))
                                {
                                    sb.AppendLine($"if (name == \"Score\")");
                                    using (sb.Block())
                                    {
                                        sb.AppendLine("// Deserialize the value directly into the property");
                                        sb.AppendLine($"result.Score = global::MongoDB.Bson.Serialization.BsonSerializer.Deserialize<float>(bsonReader);");
                                        sb.AppendLine("continue;");
                                    }
                                }


                                sb.AppendLine("if (bsonReader.State == BsonReaderState.Type)");
                                using (sb.Block())
                                {
                                    sb.AppendLine("bsonReader.ReadBsonType();");
                                }
                                sb.AppendLine("if (bsonReader.State == BsonReaderState.Type)");
                                using (sb.Block())
                                {
                                    sb.AppendLine("bsonReader.ReadName();");
                                }

                                sb.AppendLine("bsonReader.SkipValue(); // Skips fields like _id if they leak through");
                            }
                        }
                        sb.AppendLine("bsonReader.ReadEndDocument();");
                        sb.AppendLine("return result;");
                    }
                }

                context.AddSource($"{model.ModelName}_{model.Name}_BsonProjectionSerializer.g.cs", sb.ToString());
            }
        }

        private ImmutableArray<ProjectionModel>? BuildProjectionModel(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var compilation = ctx.SemanticModel.Compilation;
            var mongoObjectAttrSymbol = compilation.GetTypeByMetadataName("MongoObject.Core.Attributes.MongoObjectAttribute");
            var bsonElementAttrSymbol = compilation.GetTypeByMetadataName("MongoDB.Bson.Serialization.Attributes.BsonElementAttribute");
            var bsonIgnoreAttrSymbol = compilation.GetTypeByMetadataName("MongoDB.Bson.Serialization.Attributes.BsonIgnoreAttribute");
            var projectionAttrSymbol = compilation.GetTypeByMetadataName("MongoObject.Core.Attributes.ProjectValue");

            var classSymbol = (INamedTypeSymbol)ctx.TargetSymbol;

            var mongoAttr = classSymbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, mongoObjectAttrSymbol));

            if (mongoAttr == null)
                return null;

            var databaseName = mongoAttr.NamedArguments.FirstOrDefault(n => n.Key == "DatabaseName").Value.Value?.ToString();
            var collectionName = mongoAttr.NamedArguments.FirstOrDefault(n => n.Key == "CollectionName").Value.Value?.ToString();

            return ProcessProjections(classSymbol, bsonElementAttrSymbol, bsonIgnoreAttrSymbol, projectionAttrSymbol).ToImmutableArray();
        }

        public IEnumerable<ProjectionModel> ProcessProjections(
            INamedTypeSymbol symbol,
            INamedTypeSymbol? bsonElementAttrSymbol,
            INamedTypeSymbol? bsonIgnoreAttrSymbol,
            INamedTypeSymbol? projectionAttrSymbol)
        {
            var projections = symbol.GetMembers()
                .OfType<IPropertySymbol>()
                .SelectMany(prop => prop.GetAttributes()
                    .Where(attr => attr.AttributeClass?.Name is "ProjectValueAttribute" or "ProjectValue")
                    .Select(attr => new { Property = prop, Attribute = attr }))
                .Select(target => {
                    var isBsonElement = bsonElementAttrSymbol != null && target.Property.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, bsonElementAttrSymbol));
                    var isBsonIgnore = bsonIgnoreAttrSymbol != null && target.Property.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, bsonIgnoreAttrSymbol));
                    var dimensions = (int)(target.Property.GetAttributes().First(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, projectionAttrSymbol)).NamedArguments.FirstOrDefault(x => x.Key == "Dimensions").Value.Value ?? 1024);
                    return new
                    {
                        Name = target.Attribute?.ConstructorArguments.FirstOrDefault().Value as string ?? target.Property.Name,

                        Prop = new ProjectionPropertyModel
                        {
                            FullName = target.Property.Type.ToDisplayString(format),
                            QueryName = isBsonElement ? target.Property.GetAttributes().First(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, bsonElementAttrSymbol)).ConstructorArguments.FirstOrDefault().Value as string ?? target.Property.Name : target.Property.Name,
                            IsBsonIgnore = isBsonIgnore,
                            IsNumeric = IsNumericType(target.Property.Type),
                            Name = target.Property.Name,
                            EnumName = EnumToString(target.Attribute),
                            VectorDimensions = dimensions,
                            SimilarityType = GetSimilarityTypeName(target.Property.GetAttributes().FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, projectionAttrSymbol) && x.NamedArguments.Any(x => x.Key != null && x.Key == "Similarity"))) ?? "Cosine", //.NamedArguments.FirstOrDefault(x => x.Key == "Similarity").Value.Value as string ?? "Cosine",
                            VectorModel = target.Property.GetAttributes().First(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, projectionAttrSymbol)).NamedArguments.FirstOrDefault(x => x.Key == "VectorModel").Value.Value as string ?? "voyage-4"
                        }
                    };
                })
                .GroupBy(x => x.Name);

            foreach (var group in projections)
            {
                yield return new ProjectionModel
                {
                    Namespace = symbol.ContainingNamespace?.ToString() ?? "",
                    ModelName = symbol.Name,
                    Name = group.Key!,
                    Description = "nothing right now",
                    Properties = [.. group.Select(x => x.Prop)]
                };
            }
        }

        private static bool IsNumericType(ITypeSymbol type)
        {
            if (type.TypeKind == TypeKind.Enum) return false;

            // Handle Nullable<T> by looking at the underlying type
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                type = ((INamedTypeSymbol)type).TypeArguments[0];
            }

            return type.SpecialType switch
            {
                SpecialType.System_Int16 or
                SpecialType.System_Int32 or
                SpecialType.System_Int64 or
                SpecialType.System_Double or
                SpecialType.System_Decimal or
                SpecialType.System_Single or
                SpecialType.System_Byte or
                SpecialType.System_SByte => true,
                _ => false
            };
        }

        private static string? EnumToString(AttributeData? symbol)
        {
            if (symbol == null) return null;
            if (symbol.ConstructorArguments.Length < 2) return null;

            var type = symbol.ConstructorArguments[1];
            return EnumTypeToStringValue(type);
        }

        private static string? EnumTypeToStringValue(TypedConstant type)
        {
            object? numericValue = type.Value;

            if (type.Type is INamedTypeSymbol enumTypeSymbol && numericValue != null)
            {
                var enumMember = enumTypeSymbol.GetMembers()
                    .OfType<IFieldSymbol>()
                    .FirstOrDefault(m => m.ConstantValue?.ToString() == numericValue.ToString());

                return enumMember?.Name;
            }

            return null;
        }

        public static string GetSimilarityTypeName(AttributeData? attributeData)
        {
            if (attributeData == null) return "Cosine";
            var projectionType = attributeData.NamedArguments.Where(x => x.Key == "Similarity").FirstOrDefault().Value.Value as int? ?? 0;
            return projectionType switch
            {
                0 => "Euclidean",
                1 => "Cosine",
                2 => "DotProduct",
                _ => throw new ArgumentOutOfRangeException(nameof(projectionType), $"Unsupported projection type: {projectionType}")
            };
        }
    }
}
