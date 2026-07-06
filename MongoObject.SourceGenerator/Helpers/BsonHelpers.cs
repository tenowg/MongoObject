using Microsoft.CodeAnalysis;
using System.Linq;

namespace MongoObject.SourceGenerator.Helpers
{
    internal class BsonHelpers
    {
        public static string GetBsonTypeString(ITypeSymbol typeSymbol)
        {
            // Unwrap nullable: int? -> int
            if (typeSymbol is INamedTypeSymbol { IsGenericType: true } named &&
                named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
            {
                typeSymbol = named.TypeArguments[0];
            }

            // Use SpecialType for built-in types (most reliable)
            return typeSymbol.SpecialType switch
            {
                SpecialType.System_String => "string",
                SpecialType.System_Int32 => "int",
                SpecialType.System_Int64 => "long",
                SpecialType.System_Double => "double",
                SpecialType.System_Boolean => "bool",
                SpecialType.System_Single => "double", // float -> double in BSON
                SpecialType.System_Decimal => "decimal",
                _ => GetBsonTypeByFullName(typeSymbol)
            };
        }

        private static string GetBsonTypeByFullName(ITypeSymbol typeSymbol)
        {
            // 1. High-priority explicit BSON/System types
            var fullName = $"{typeSymbol.ContainingNamespace}.{typeSymbol.Name}";
            var result = fullName switch
            {
                "System.DateTime" => "date",
                "System.DateTimeOffset" => "date",
                "System.Guid" => "binData",
                "MongoDB.Bson.ObjectId" => "objectId",
                "MongoDB.Bson.Decimal128" => "decimal",
                "MongoDB.Bson.BsonDocument" => "object",
                "MongoDB.Bson.BsonArray" => "array",
                _ => null
            };

            if (result != null) return result;

            // 2. Arrays (e.g., string[])
            if (typeSymbol.TypeKind == TypeKind.Array)
                return "array";

            // 3. Check for Dictionary/Map (BSON "object")
            // Dictionaries are objects in Mongo because they have key-value pairs
            if (ImplementsInterface(typeSymbol, "System.Collections.Generic.IDictionary"))
                return "object";

            // 4. Check for List/Enumerable (BSON "array")
            // This catches List<T>, IEnumerable<T>, ICollection<T>, etc.
            if (ImplementsInterface(typeSymbol, "System.Collections.Generic.IEnumerable"))
                return "array";

            // 5. Enums (usually stored as Int32/int)
            if (typeSymbol.TypeKind == TypeKind.Enum)
                return "int";

            return "object";
        }

        private static bool ImplementsInterface(ITypeSymbol type, string interfaceFullName)
        {
            // Check the type itself if it's an interface (e.g. IEnumerable<string> prop)
            if (IsInterface(type, interfaceFullName)) return true;

            // Check all implemented interfaces
            return type.AllInterfaces.Any(i => IsInterface(i, interfaceFullName));
        }

        private static bool IsInterface(ITypeSymbol type, string name)
        {
            // Check original definition to handle generic versions comfortably
            var fullName = $"{type.ContainingNamespace}.{type.Name}";
            return fullName == name;
        }
    }
}
