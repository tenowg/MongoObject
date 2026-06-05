using System.Collections.Generic;

namespace MongoObject.SourceGenerator.Models
{
    /// <summary>
    /// Equatable document model for incremental source generation.
    /// Contains no Roslyn symbols to ensure proper caching behavior.
    /// </summary>
    internal sealed record CommonModel
    {
        public string Namespace { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string DatabaseName { get; init; } = string.Empty;
        public string CollectionName { get; init; } = string.Empty;
        public bool BsonValidation { get; init; } = false;
        public IReadOnlyList<PropertyModel> Properties { get; init; } = [];
        public MetadataModel Metadata { get; init; } = new();
        public IReadOnlyList<ProjectionModel> Projections { get; init; } = [];
        public IReadOnlyList<ValidationResult> Errors { get; init; } = [];
        public IReadOnlyList<IndexModel> Indexes { get; init; } = [];
    }
}