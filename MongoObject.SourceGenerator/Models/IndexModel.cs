using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace MongoObject.SourceGenerator.Models
{
    internal sealed record IndexModel
    {
        public string Name { get; init; } = string.Empty;
        public IReadOnlyList<PropertyModel> Properties { get; init; } = [];
        public bool IsUnique { get; init; }
    }
}
