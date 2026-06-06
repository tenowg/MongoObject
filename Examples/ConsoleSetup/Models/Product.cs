#region Product
using MongoDB.Bson.Serialization.Attributes;
using MongoObject.Core.Attributes;

namespace ConsoleSetup.Models
{
    public record ProductMeta
    {
        public string? Department { get; set; } = string.Empty;
    }

    [MongoObject(
        CollectionName = "Products",
        DatabaseName = "MyStore",
        MetadataType = typeof(ProductMeta)
    )]
    public partial class Product
    {
        [BsonElement("name")]
        public partial string Name { get; set; }
        public partial decimal Price { get; set; }
        public partial string Description { get; set; }
        public partial int StockQuantity { get; set; }
        public partial List<string> Tags { get; set; }
    }

    #endregion
}