using MongoObject.SourceGenerator.Encryption.Interfaces;

namespace MongoObject.SourceGenerator.Encryption.Models
{
    internal sealed record PropertyModel : IPropertyModel
    {
        public string Name { get; init; } = string.Empty;
        public LocalModel? Local { get; init; }
        public AwsModel? Aws { get; init; }
        public AzureModel? Azure { get; init; }
    }

    internal sealed record EncryptedPropertyModel : IPropertyModel
    {
        public string? Name { get; init; }
        public bool IsEncrypted { get; init; }
    }
}
