namespace MongoObject.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MongoObjectAttribute : Attribute
    {
        public string? CollectionName { get; set; }
        public string? DatabaseName { get; set; }
        public Type? MetadataType { get; set; }
    }
}
