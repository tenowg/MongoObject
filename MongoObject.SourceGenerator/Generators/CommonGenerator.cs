using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;
using MongoObject.SourceGenerator.Modules;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace MongoObject.SourceGenerator.Generators
{
    [Generator]
    internal partial class CommonGenerator : IIncrementalGenerator
    {
        private static readonly ICodeModule[] _modules =
        [
            new ValidatorModule(),
            new MetadataModule(),
            new MongoObjectModule(),
            new DocumentSearchModule(),
            new ExtensionModule(),
            new ProjectionModule(),
            new SearchBuilderModule(),
            new AddBuilderModule(),
            new DeleteManyBuilderModule(),
            
        ];

        private static readonly ICodeModuleMultiple[] _modulesMultiple =
        [
            new ObjectDiscoveryModule(),
            new MongoIndexModule()
        ];

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //Debugger.Launch();
            var provider = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                    transform: (ctx, ct) => BuildCommonModel(ctx, ct))
                .Where(static m => m is not null);

            var compilations = provider.Combine(context.CompilationProvider);

            var values = context.AnalyzerConfigOptionsProvider.Select(static (options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
                return rootNamespace ?? "DefaultName";
            });

            context.RegisterSourceOutput(compilations, static (spc, model) =>
            {
                foreach (var module in _modules)
                {
                    spc.CancellationToken.ThrowIfCancellationRequested();
                    module.Execute(spc, model!);
                }
            });

            var combinedProvider = provider.Collect().Combine(values);

            context.RegisterSourceOutput(combinedProvider, static (spc, models) =>
            {
                foreach (var module in _modulesMultiple)
                {
                    spc.CancellationToken.ThrowIfCancellationRequested();
                    module.Execute(spc, models);
                }
            });
        }

        public static CommonModel? BuildCommonModel(GeneratorSyntaxContext ctx, System.Threading.CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var classDecl = (ClassDeclarationSyntax)ctx.Node;
            var symbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl, ct);

            if (symbol is not INamedTypeSymbol namedTypeSymbol)
                return null;

            var mongoAttr = symbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "MongoObjectAttribute");

            if (mongoAttr == null)
                return null;

            // Pre-resolve symbols needed for property analysis
            var compilation = ctx.SemanticModel.Compilation;
            var trackingBaseSymbol = compilation.GetTypeByMetadataName("MongoObject.Core.Data.TrackingObservableObject");
            var mongoObjectAttrSymbol = compilation.GetTypeByMetadataName("MongoObject.Core.Attributes.MongoObjectAttribute");
            var indexAttrSymbol = compilation.GetTypeByMetadataName("MongoObject.Core.Attributes.MongoIndexAttribute");

            var databaseName = mongoAttr.NamedArguments.FirstOrDefault(n => n.Key == "DatabaseName").Value.Value?.ToString();
            var collectionName = mongoAttr.NamedArguments.FirstOrDefault(n => n.Key == "CollectionName").Value.Value?.ToString();

            var metaTypeName = $"{symbol.Name}DefaultMeta";
            var metaType = symbol.GetAttributes()
                .Select(a => a.NamedArguments.FirstOrDefault(n => n.Key == "MetadataType").Value)
                .FirstOrDefault();

            var errors = new List<ValidationResult>();

            var metadata = new MetadataModel
            {
                Name = metaTypeName
            };

            if (!metaType.IsNull && metaType.Value is INamedTypeSymbol metaTypeSymbol)
            {
                metadata = new MetadataModel
                {
                    Name = metaTypeSymbol.Name,
                    Properties = metaTypeSymbol.GetMembers()
                        .OfType<IPropertySymbol>()
                        .Where(c => !c.Name.StartsWith("EqualityContract"))
                        .Select(x => new PropertyModel
                        {
                            FullName = x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            Name = x.Name,
                            IsNumeric = IsNumericType(x.Type)
                        })
                        .ToImmutableArray()
                };

                errors.AddRange(metaTypeSymbol.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(c => c.Name.StartsWith("Version") || c.Name.StartsWith("LastModifiedAt") || c.Name.StartsWith("CreatedAt"))
                    .Select(x => new ValidationResult(true, x.Locations.FirstOrDefault(), new List<string> { x.Name, x.Type.Name }, DeclaredDiagnosticDescriptor.InvalidPropertyNameReservedDescriptor)));

                errors.AddRange(metaTypeSymbol.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(c => !IsNullable(c.Type) && !c.Name.StartsWith("EqualityContract"))
                    .Select(x => new ValidationResult(true, x.Locations.FirstOrDefault(), new List<string> { x.Name, x.Type.Name }, DeclaredDiagnosticDescriptor.InvalidPropertyNonNullableDescriptor)));
            }

            // Process properties and validate non-partial properties
            var (validProperties, invalidProperties) = ProcessAllProperties(namedTypeSymbol, trackingBaseSymbol, mongoObjectAttrSymbol, indexAttrSymbol);

            Dictionary<string, List<PropertyModel>> indexes = [];
            foreach(var prop in validProperties)
            {
                if (prop.IsMongoIndex)
                {
                    // first lets build the list
                    foreach(var index in prop.Indexes)
                    {
                        if (!indexes.ContainsKey(index.IndexName))
                        {
                            indexes[index.IndexName] = new List<PropertyModel>();
                        }
                        indexes[index.IndexName].Add(prop);
                    }
                }
            }

            errors.AddRange(invalidProperties.Select(x => new ValidationResult(true, x.Locations.FirstOrDefault(), new List<string> { x.Name, x.Type.Name }, DeclaredDiagnosticDescriptor.InvalidPropertyTypeDescriptor)));

            return new CommonModel
            {
                Namespace = symbol.ContainingNamespace?.ToString() ?? "",
                Name = symbol.Name,
                DatabaseName = databaseName ?? "",
                CollectionName = collectionName ?? symbol.Name,
                Metadata = metadata,
                Properties = validProperties,
                Projections = ProcessProjections(namedTypeSymbol).ToImmutableArray(),
                Errors = errors,
                Indexes = indexes.Select(g => new IndexModel { Name = g.Key, Properties = g.Value.ToImmutableArray() }).ToImmutableArray()
            };
        }

        /// <summary>
        /// Single-pass property processing that separates valid partial properties from invalid ones.
        /// Pre-computes all symbol-based checks for equatable model building.
        /// </summary>
        private static (ImmutableArray<PropertyModel> validProperties, IEnumerable<IPropertySymbol> invalidProperties) ProcessAllProperties(
            INamedTypeSymbol symbol,
            INamedTypeSymbol? trackingBaseSymbol,
            INamedTypeSymbol? mongoObjectAttrSymbol,
            INamedTypeSymbol? mongoIndexAttrSymbol)
        {
            var validProperties = ImmutableArray.CreateBuilder<PropertyModel>();
            var invalidProperties = new List<IPropertySymbol>();

            var properties = symbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public
                            && !p.IsStatic
                            && p.SetMethod is not null
                            && p.GetMethod is not null);

            foreach (var prop in properties)
            {
                if (!prop.IsPartialDefinition)
                {
                    // Non-partial public property with getter/setter is invalid
                    invalidProperties.Add(prop);
                    continue;
                }

                // Pre-compute symbol-based checks
                var isMongoObject = mongoObjectAttrSymbol != null && HasAttribute(prop.Type, mongoObjectAttrSymbol);
                var isMongoIndex = mongoIndexAttrSymbol != null && prop.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, mongoIndexAttrSymbol));
                var isTrackable = trackingBaseSymbol != null && InheritsFrom(prop.Type, trackingBaseSymbol);
                var isComplexUntracked = !isMongoObject && !isTrackable && IsComplexUntrackedClass(prop.Type);

                var (typeName, isNullable, underlyingTypeName) = GetTypeInfo(prop.Type);

                var indexName = new List<MongoIndexPropertyModel>();
                if (isMongoIndex)
                {
                    indexName = prop.GetAttributes()
                        .Where(prop => SymbolEqualityComparer.Default.Equals(prop.AttributeClass, mongoIndexAttrSymbol))
                        .Select(a => new MongoIndexPropertyModel
                        {
                            IndexName = a.ConstructorArguments.FirstOrDefault().Value as string,
                            Name = prop.Name,
                            Order = a.NamedArguments.Where(x => x.Key == "Type").FirstOrDefault().Value.Value as int? == 0 ? "Ascending" : "Descending",
                            Unique = a.NamedArguments.Where(x => x.Key == "Unique").FirstOrDefault().Value.Value as bool? ?? false,
                        })
                        .ToList();
                }

                validProperties.Add(new PropertyModel
                {
                    FullName = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    Name = prop.Name,
                    IsNumeric = IsNumericType(prop.Type),
                    IsMongoObject = isMongoObject,
                    IsTrackable = isTrackable,
                    IsComplexUntrackedClass = isComplexUntracked,
                    TypeName = typeName,
                    IsNullable = isNullable,
                    UnderlyingTypeName = underlyingTypeName,
                    IsMongoIndex = isMongoIndex,
                    Indexes = indexName
                });
            }

            return (validProperties.ToImmutable(), invalidProperties);
        }

        private static bool HasAttribute(ITypeSymbol type, INamedTypeSymbol attributeType)
        {
            return type.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType));
        }

        private static bool InheritsFrom(ITypeSymbol type, INamedTypeSymbol baseType)
        {
            if (type is not INamedTypeSymbol namedType)
                return false;

            var current = namedType.BaseType;
            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                    return true;
                current = current.BaseType;
            }

            return false;
        }

        private static bool IsComplexUntrackedClass(ITypeSymbol typeSymbol)
        {
            if (!typeSymbol.IsReferenceType)
                return false;

            if (typeSymbol.SpecialType != SpecialType.None)
                return false;

            return true;
        }

        public static bool IsNullable(ITypeSymbol typeSymbol)
        {
            if (typeSymbol.IsValueType)
            {
                return typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
            }

            return typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
        }

        public static IEnumerable<ProjectionModel> ProcessProjections(INamedTypeSymbol symbol)
        {
            var projections = symbol.GetMembers()
                .OfType<IPropertySymbol>()
                .SelectMany(prop => prop.GetAttributes()
                    .Where(attr => attr.AttributeClass?.Name is "ProjectValueAttribute" or "ProjectValue")
                    .Select(attr => new { Property = prop, Attribute = attr }))
                .Select(target => new
                {
                    Name = target.Attribute?.ConstructorArguments.FirstOrDefault().Value as string ?? target.Property.Name,
                    Prop = new PropertyModel
                    {
                        FullName = target.Property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        IsNumeric = IsNumericType(target.Property.Type),
                        Name = target.Property.Name,
                        EnumName = EnumToString(target.Attribute)
                    }
                })
                .GroupBy(x => x.Name);

            foreach (var group in projections)
            {
                yield return new ProjectionModel
                {
                    Name = group.Key!,
                    Description = "nothing right now",
                    Properties = group.Select(x => x.Prop).ToImmutableArray()
                };
            }
        }
    }
}