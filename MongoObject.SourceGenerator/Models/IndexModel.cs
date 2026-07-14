using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace MongoObject.SourceGenerator.Models
{
    internal sealed record IndexModel
    {
        public string Name { get; init; } = string.Empty;
        public IReadOnlyList<IndexProperty> Properties { get; init; } = [];
        public bool IsUnique { get; init; }
        public string DatabaseName { get; init; } = string.Empty;
        public string CollectionName { get; init; } = string.Empty;
    }

    internal sealed record IndexModelProvider
    {
        public string DatabaseName { get; init; } = string.Empty;
        public string CollectionName { get; init; } = string.Empty;
        public ImmutableArray<IndexModel> indexModels;
    }
}
