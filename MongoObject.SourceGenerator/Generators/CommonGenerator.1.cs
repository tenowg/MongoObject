using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MongoObject.SourceGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoObject.SourceGenerator.Generators
{
    internal partial class CommonGenerator
    {
        /// <summary>
        /// Extracts type information for code generation.
        /// Returns (typeName, isNullable, underlyingTypeName).
        /// </summary>
        private static (string typeName, bool isNullable, string? underlyingTypeName) GetTypeInfo(ITypeSymbol type)
        {
            var typeName = type.Name;
            var isNullable = false;
            string? underlyingTypeName = null;

            // Check for Nullable<T>
            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                if (namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    isNullable = true;
                    var typeArg = namedType.TypeArguments[0];
                    underlyingTypeName = typeArg.Name;
                    typeName = typeArg.Name;
                }
            }
            // Check for reference type with nullable annotation
            else if (type.NullableAnnotation == NullableAnnotation.Annotated)
            {
                isNullable = true;
            }

            return (typeName, isNullable, underlyingTypeName);
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

        public static string GetProjectionTypeName(AttributeData attributeData)
        {
            var projectionType = attributeData.NamedArguments.Where(x => x.Key == "Type").FirstOrDefault().Value.Value as int? ?? 0;
            return projectionType switch
            {
                0 => "Include",
                1 => "Exclude",
                2 => "Slice",
                3 => "Vector",
                4 => "AutoVector",
                _ => throw new ArgumentOutOfRangeException(nameof(projectionType), $"Unsupported projection type: {projectionType}")
            };
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
