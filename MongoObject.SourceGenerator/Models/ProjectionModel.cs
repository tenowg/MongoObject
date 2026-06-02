using System.Collections.Generic;

namespace MongoObject.SourceGenerator.Models
{
    /// <summary>
    /// Equatable projection model for incremental source generation.
    /// </summary>
    internal sealed record ProjectionModel
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public IReadOnlyList<PropertyModel> Properties { get; init; } = [];
    }
}