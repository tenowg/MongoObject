using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoObject.Core.Attributes;
using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;
using System.Collections.ObjectModel;

namespace Progress.Test
{
    public partial record AObjectMeta
    {
        public int? Id { get; set; }
        public string? OwnerId { get; set; }
    }

    [MongoObject(CollectionName = "AObjectsCollection", DatabaseName = "AObjectsDatabase", MetadataType = typeof(AObjectMeta))]
    [BsonIgnoreExtraElements]
    public partial class AObject
    {
        [ProjectValue("NameProjection", ProjectionType.Include)]
        [ProjectValue("OtherProjection", ProjectionType.Exclude)]
        public partial string Name { get; set; }
        [ProjectValue("OtherProjection", ProjectionType.Include)]
        public partial int Age { get; set; }
        public partial BObject Nothing { get; set; }
        public partial ObservableCollection<string> Tags { get; set; }
        public partial TestA test { get; set; }
        public partial Dictionary<string, string> Properties { get; set; }
    }

    public class TestA
    {
        public string TestString { get; set; }
    }

    [MongoObject(DatabaseName = "BObjectsDatabase")]
    public partial class BObject
    {
        public partial string Name { get; set; }
        public partial int Age { get; set; }
        public partial BObject Nothing { get; set; }
    }
}
