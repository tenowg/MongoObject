using MongoDB.Driver;

namespace MongoObject.Core.Attributes
{
    public enum IndexDirection
    {
        Ascending = 0,
        Descending = 1
    };

    public enum IndexType
    {
        Index = 0,
        Text = 1
    }

    /// <summary>
    /// MongoIndex attribute is used to mark a property as an index in MongoDB. It allows you to specify the index name, type (ascending or descending), description, and whether the index is unique.
    /// If two properties have the same index name, they will be combined into a compound index.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class FieldIndexAttribute(string IndexName) : Attribute
    {
        public string IndexName { get; set; } = IndexName;
        public IndexDirection Direction { get; set; } = IndexDirection.Ascending;
        public IndexType Type { get; set; } = IndexType.Index;
    }
}
