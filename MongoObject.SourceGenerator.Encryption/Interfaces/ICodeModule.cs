using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Encryption.Models;
using System.Collections.Immutable;
using System.Linq;

namespace MongoObject.SourceGenerator.Encryption.Interfaces
{
    internal interface ICodeModule
    {
        void Execute(SourceProductionContext context, ((CommonModel model, EncryptedClassModel encrypted) models, Compilation comp) provider);
    }

    internal interface ICodeModuleMultiple
    {
        void Execute(SourceProductionContext context, (ImmutableArray<(CommonModel?, EncryptedClassModel?)> models, string rootNamespace) args);
    }

    internal abstract class CodeModule : ICodeModule
    {
        public abstract void Execute(SourceProductionContext context, ((CommonModel model, EncryptedClassModel encrypted) models, Compilation comp) provider);

        protected static bool IsTypeEqual(ITypeSymbol source, INamedTypeSymbol target)
        {
            return SymbolEqualityComparer.Default.Equals(source, target);
        }

        protected static bool PropertyTypeEquals(IPropertySymbol source, INamedTypeSymbol target)
        {
            return source.Type.GetAttributes().Any(x => x.AttributeClass != null && IsTypeEqual(x.AttributeClass, target));
        }

        protected static bool IsType(ITypeSymbol source, INamedTypeSymbol target)
        {
            if (SymbolEqualityComparer.Default.Equals(source, target))
                return true;

            if (source is INamedTypeSymbol namedPropertyType)
            {
                var currentBase = namedPropertyType.BaseType;
                while (currentBase != null)
                {
                    if (SymbolEqualityComparer.Default.Equals(currentBase, target))
                        return true;
                    currentBase = currentBase.BaseType;
                }
            }

            return false;
        }

        protected static bool IsComplexUntrackedClass(ITypeSymbol typeSymbol)
        {
            if (!typeSymbol.IsReferenceType)
                return false;

            if (typeSymbol.SpecialType != SpecialType.None)
                return false;

            return true;
        }
    }
}