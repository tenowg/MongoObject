using System.Collections.Generic;

namespace MongoObject.SourceGenerator.Models
{
    /// <summary>
    /// Equatable property model for incremental source generation.
    /// Contains no Roslyn symbols to ensure proper caching behavior.
    /// </summary>
    internal sealed record PropertyModel
    {
        public string Name { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string QueryName { get; init; } = string.Empty;
        public bool IsBsonIgnore { get; init; }
        public bool IsNumeric { get; init; }
        public string? EnumName { get; init; }
        
        /// <summary>
        /// Pre-computed: indicates if this property type has [MongoObject] attribute.
        /// </summary>
        public bool IsMongoObject { get; init; }
        
        /// <summary>
        /// Pre-computed: indicates if this is a complex class that needs change registration.
        /// </summary>
        public bool IsComplexUntrackedClass { get; init; }
        
        /// <summary>
        /// Pre-computed: indicates if this property type inherits from TrackingObservableObject.
        /// </summary>
        public bool IsTrackable { get; init; }
        
        /// <summary>
        /// The simple type name (without namespace or nullable annotation).
        /// Used for generating query types like "BObjectQuery" from property type "BObject".
        /// </summary>
        public string TypeName { get; init; } = string.Empty;
        
        /// <summary>
        /// Indicates if the property type is nullable (Nullable<T> or T?).
        /// </summary>
        public bool IsNullable { get; init; }
        
        /// <summary>
        /// For nullable types, the underlying type name (e.g., "int" for "int?").
        /// </summary>
        public string? UnderlyingTypeName { get; init; }
        
        public bool IsMongoIndex { get; init; }

        public IReadOnlyList<MongoIndexPropertyModel> Indexes { get; init; } = [];
        public int VectorDimensions { get; init; } = 1024;
        public string SimilarityType { get; set; } = "Cosine";
        public string VectorModel { get; set; } = string.Empty;
        public bool isEncrypted { get; set; } = false;
    }

    internal sealed record MongoIndexPropertyModel
    {
        public string? IndexName { get; init; }
        public string? Name { get; init; }
        public bool? Unique { get; init; }
        public string? Order { get; init; }
    }
}