using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MongoObject.SourceGenerator.Encryption.Interfaces;
using MongoObject.SourceGenerator.Encryption.Models;
using MongoObject.SourceGenerator.Encryption.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MongoObject.SourceGenerator.Encryption.Generators
{
    [Generator]
    internal class EncryptionGenerator : IIncrementalGenerator
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
        private static readonly ICodeModule[] _modules =
        [
            new AttributeModule(),
            new ValidatorModule()
        ];

        private static readonly ICodeModuleMultiple[] _modulesMultiple =
        [
            new ModuleInitializationModule()
        ];

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //Debugger.Launch();
            var provider = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                    transform: (ctx, ct) => BuildCommonModel(ctx, ct))
                .Where(static m => m is not (null, null));

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

        private (CommonModel?, EncryptedPropertyModel?) BuildCommonModel(GeneratorSyntaxContext ctx, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var classDecl = (ClassDeclarationSyntax)ctx.Node;
            var symbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl, ct);

            if (symbol is not INamedTypeSymbol namedTypeSymbol)
                return (null, null);

            var compilation = ctx.SemanticModel.Compilation;
            var kmsProvidersAttrSymbol = compilation.GetTypeByMetadataName("MongoObject.PropertyEncryption.Attributes.KMSProvidersAttribute");
            var mongoEncryptAttrSymbol = compilation.GetTypeByMetadataName("MongoObject.PropertyEncryption.Attributes.MongoEncryptAttribute");

            var kmsLocalAttrSymbol = compilation.GetTypeByMetadataName("MongoObject.PropertyEncryption.Attributes.KMSLocalAttribute");
            var kmsAwsAttrSymbol = compilation.GetTypeByMetadataName("MongoObject.PropertyEncryption.Attributes.KMSAwsAttribute");
            var kmsAzureAttrSymbol = compilation.GetTypeByMetadataName("MongoObject.PropertyEncryption.Attributes.KMSAzureAttribute");

            var kmsProviderAttr = symbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, kmsProvidersAttrSymbol));
            var mongoEncryptAttr = symbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, mongoEncryptAttrSymbol));

            if (kmsProviderAttr == null && mongoEncryptAttr == null)
                return (null, null);

            CommonModel? commonModel = null;
            EncryptedPropertyModel? encryptionModel = null;
            var (valid, invalid) = ProcessProperty(namedTypeSymbol, kmsLocalAttrSymbol, kmsAwsAttrSymbol, kmsAzureAttrSymbol);
            
            if (kmsProviderAttr != null)
            {
                commonModel = new CommonModel
                {
                    Name = symbol.Name,
                    FullQualifiedName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    Namespace = symbol.ContainingNamespace?.ToString() ?? "",
                    Errors = invalid,
                    Properties = valid
                };
            }

            if (mongoEncryptAttr != null)
            {
                encryptionModel = new EncryptedPropertyModel
                {
                    FullQualifiedName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    Properties = valid
                };
            }

            return (commonModel, encryptionModel);
        }

        public (List<PropertyModel> valid, List<ValidationResult> invalid) ProcessProperty(INamedTypeSymbol symbol, INamedTypeSymbol? kmsLocalAttr, INamedTypeSymbol? kmsAwsAttr, INamedTypeSymbol? kmsAzureAttr)
        {
            var valid = new List<PropertyModel>();
            var invalid = new List<ValidationResult>();

            var properties = symbol.GetMembers().OfType<IPropertySymbol>().Where(x => x.DeclaredAccessibility == Accessibility.Public
                            && !x.IsStatic
                            && x.SetMethod is not null
                            && x.GetMethod is not null);

            foreach (var property in properties)
            {
                var isLocal = property.GetAttributes().Where(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, kmsLocalAttr)).FirstOrDefault();
                var isAws = property.GetAttributes().Where(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, kmsAwsAttr)).FirstOrDefault();
                var isAzure = property.GetAttributes().Where(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, kmsAzureAttr)).FirstOrDefault();

                if (new[] { isLocal is not null, isAws is not null, isAzure is not null }.Count(x => x) > 1)
                { 
                    invalid.Add(new ValidationResult(true, property.Locations.First(), [property.Name], DeclaredDiagnosticDescriptor.InvalidKmsDoubleAttributeDescriptor));
                    continue;
                }

                if (new[] { isLocal is not null, isAws is not null, isAzure is not null }.Count(x => x) != 0)
                {
                    valid.Add(new PropertyModel
                    {
                        Name = property.Name,
                        Local = isLocal is null ? null : new LocalModel
                        {
                            Key = isLocal.NamedArguments.FirstOrDefault(n => n.Key == "Key").Value.Value?.ToString() ?? property.Name.ToLowerInvariant(),
                            BinFilePath = isLocal is null ? string.Empty : isLocal.ConstructorArguments[0].Value?.ToString() ?? string.Empty
                        },
                        Aws = isAws is null ? null : new AwsModel
                        {
                            Key = isAws.NamedArguments.FirstOrDefault(n => n.Key == "Key").Value.Value?.ToString() ?? property.Name.ToLowerInvariant(),
                            SessionTokenPath = isAws.NamedArguments.FirstOrDefault(n => n.Key == "SessionTokenPath").Value.Value?.ToString(),
                            SecretKeyPath = isAws.NamedArguments.FirstOrDefault(n => n.Key == "SecretKeyPath").Value.Value?.ToString(),
                            AccessKeyPath = isAws.NamedArguments.FirstOrDefault(n => n.Key == "AccessKeyPath").Value.Value?.ToString()
                        },
                        Azure = isAzure is null ? null : new AzureModel
                        {
                            Key = isAzure.NamedArguments.FirstOrDefault(n => n.Key == "Key").Value.Value?.ToString() ?? property.Name.ToLowerInvariant(),
                            TenantIdPath = isAzure.NamedArguments.FirstOrDefault(n => n.Key == "TenantIdPath").Value.Value?.ToString() ?? property.Name.ToLowerInvariant(),
                            ClientIdPath = isAzure.NamedArguments.FirstOrDefault(n => n.Key == "ClientIdPath").Value.Value?.ToString() ?? property.Name.ToLowerInvariant(),
                            ClientSecretPath = isAzure.NamedArguments.FirstOrDefault(n => n.Key == "ClientSecretPath").Value.Value?.ToString() ?? property.Name.ToLowerInvariant()
                        }
                    });
                }
            }

            return (valid, invalid);
        }
    }
}
