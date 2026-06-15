namespace MongoObject.SourceGenerator.Encryption.Models
{
    internal sealed record LocalModel
    {
        public string? Key { get; set; }
        public string? BinFilePath { get; set; }
    }
}
