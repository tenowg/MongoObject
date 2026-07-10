using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoObject.Core.Attributes;
using MongoObject.PropertyEncryption.Attributes;
using Progress.Kms;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Progress.Test
{
    public partial record AObjectMeta
    {
        public int? Id { get; set; }
        public string? OwnerId { get; set; }
    }

    [MongoObject(CollectionName = "AObjectsCollection", DatabaseName = "AObjectsDatabase", MetadataType = typeof(AObjectMeta))]
    [MigrationSchema(OrphanFieldPolicy.AlwaysAsk)]
    public partial class AObject
    {
        [ProjectValue("Name", ProjectionType.Include)]
        [ProjectValue("Other", ProjectionType.Exclude)]
        [ProjectValue("VectorTest", ProjectionType.AutoVector, Similarity = VectorSimilarity.Cosine)]
        [MongoIndex("NameIndex", Type = IndexType.Ascending, Unique = true)]
        [Required]
        //[BsonElement("name")]
        public partial string Name { get; set; }
        [ProjectValue("Other", ProjectionType.Include)]
        [MongoIndex("NameIndex", Type = IndexType.Ascending, Unique = true)]
        [MongoIndex("AgeIndex", Type = IndexType.Descending)]
        public partial int Age { get; set; }
        public partial ObservableCollection<string> Tags { get; set; } = [];
        public partial BObject test { get; set; } = new();
        //public partial Dictionary<string, string> Properties { get; set; }
        [ProjectValue("ListTest", ProjectionType.Slice)]
        public partial List<string> ListTest { get; set; } = [];
    }

    public class TestA
    {
        public string? TestString { get; set; }
    }

    [MongoObject(DatabaseName = "BObjectsDatabase")]
    [MongoEncrypt("local")]
    public partial class BObject
    {
        [BsonElement("user_name")]
        [EncyptedField]
        public partial string Name { get; set; }
        [EncyptedField]
        public partial int Age { get; set; }
        public partial TestA Test { get; set; } = new();
    }

    [MongoObject]
    public partial class CObject
    {
        public partial string Name { get; set; } = string.Empty;
        [BsonElement("last")]
        public partial int Last { get; set; }
        public partial int Age { get; set; }
    }
}
