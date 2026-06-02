using System.Collections.Generic;

namespace MongoObject.SourceGenerator.Models
{
    /// <summary>
    /// Equatable metadata model for incremental source generation.
    /// </summary>
    internal sealed record MetadataModel
    {
        public string Name { get; init; } = string.Empty;
        public IReadOnlyList<PropertyModel> Properties { get; init; } = [];
    }
}