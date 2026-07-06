using MongoObject.SourceGenerator.Encryption.Interfaces;
using System.Collections.Generic;

namespace MongoObject.SourceGenerator.Encryption.Models
{
    internal sealed record EncryptedClassModel : IPropertyModel
    {
        public string Name { get; init; } = string.Empty;
        public string FullQualifiedName {  get; init; } = string.Empty;
        public string? ProviderKey {  get; init; } = string.Empty;
        public IReadOnlyList<EncryptedPropertyModel> Properties { get; set; } = [];
        public IReadOnlyList<ValidationResult> Errors { get; set; } = [];
    }
}
