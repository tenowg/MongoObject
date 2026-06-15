using System.Collections.Generic;

namespace MongoObject.SourceGenerator.Encryption.Models
{
    internal sealed record CommonModel
    {
        public string Name { get; init; } = string.Empty;
        public string FullQualifiedName { get; init; } = string.Empty;
        public string Namespace { get; set;  } = string.Empty;
        public IReadOnlyList<PropertyModel> Properties { get; set; } = [];
        public IReadOnlyList<ValidationResult> Errors { get; set; } = [];
    }
}
