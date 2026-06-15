namespace MongoObject.PropertyEncryption.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class KMSAzureAttribute : Attribute
    {
        public string Key { get; set; } = string.Empty;
        public string? TenantIdPath { get; set; }
        public string? ClientIdPath { get; set; }
        public string? ClientSecretPath { get; set; }
    }
}
