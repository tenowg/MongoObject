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

            var kmsProviderAttr = symbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, kmsProvidersAttrSymbol));
            var mongoEncryptAttr = symbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, mongoEncryptAttrSymbol));

            if (kmsProviderAttr == null && mongoEncryptAttr == null)
                return (null, null);

            CommonModel? commonModel = null;
            EncryptedPropertyModel? encryptionModel = null;

            if (kmsProviderAttr != null)
            {
                commonModel = new CommonModel
                {
                    FullQualifiedName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                };
            }

            if (mongoEncryptAttr != null)
            {
                encryptionModel = new EncryptedPropertyModel { 
                    FullQualifiedName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) 
                };
            }

            return (commonModel, encryptionModel);
        }
    }
}
