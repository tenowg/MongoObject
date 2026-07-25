using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.SourceGenerator.Models
{
    internal sealed record ProjectionPropertyModel
    {
        public string Name { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string QueryName { get; init; } = string.Empty;
        public bool IsBsonIgnore { get; init; }
        public bool IsNumeric { get; init; }
        public string? EnumName { get; init; }
        public int VectorDimensions { get; init; } = 1024;
        public string SimilarityType { get; set; } = "Cosine";
        public string VectorModel { get; set; } = string.Empty;
    }
}
