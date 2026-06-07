using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoObject.Core.Attributes;
using System.Collections.ObjectModel;

namespace Progress.Test
{
    public partial record AObjectMeta
    {
        public int? Id { get; set; }
        public string? OwnerId { get; set; }
    }

    [MongoObject(CollectionName = "AObjectsCollection", DatabaseName = "AObjectsDatabase", MetadataType = typeof(AObjectMeta))]
    public partial class AObject
    {
        [ProjectValue("Name", ProjectionType.Include)]
        [ProjectValue("Other", ProjectionType.Exclude)]
        [ProjectValue("VectorTest", ProjectionType.AutoVector)]
        [MongoIndex("NameIndex", Type = IndexType.Ascending, Unique = true)]
        //[BsonElement("name")]
        public partial string Name { get; set; }
        [ProjectValue("Other", ProjectionType.Include)]
        [MongoIndex("NameIndex", Type = IndexType.Ascending, Unique = true)]
        [MongoIndex("AgeIndex", Type = IndexType.Descending)]
        public partial int Age { get; set; }
        //[BsonIgnore]
        public partial BObject Nothing { get; set; }
        public partial ObservableCollection<string> Tags { get; set; }
        public partial TestA test { get; set; }
        public partial Dictionary<string, string> Properties { get; set; }
        [ProjectValue("ListTest", ProjectionType.Slice)]
        public partial List<string> ListTest { get; set; } = [];
    }

    public class TestA
    {
        public string? TestString { get; set; }
    }

    [MongoObject(DatabaseName = "BObjectsDatabase")]
    public partial class BObject
    {
        [MongoIndex("BNameIndex", Type = IndexType.Ascending, Unique = true)]
        public partial string Name { get; set; }
        public partial int Age { get; set; }
    }

    //public class AObjectNameProjectionT : global::MongoObject.Core.Interfaces.IProjectionBase, global::MongoObject.Core.Interfaces.IProjectionBase<global::Progress.Test.AObject>, IAObjectNameProjectionT1<global::Progress.Test.AObject, AObjectNameProjectionT>
    //{
    //    public string? Name { get; set; }
    //    public List<string> ExtraElements { get; set; } = new List<string>();

    //    public ProjectionDefinition<MongoDocument<AObject>, AObjectNameProjectionT> ToMongoProjection(string prefix = "")
    //    {
    //        var builder = global::MongoDB.Driver.Builders<global::MongoObject.Core.Data.MongoDocument<global::Progress.Test.AObject>>.Projection;

    //        var expression = builder.Expression(u => new AObjectNameProjectionT
    //        {
    //            Name = u.Document.Name,
    //            ExtraElements = u.Document.ListTest.Slice(0, 2)
    //        });

    //        return expression;
    //    }

    //    ProjectionDefinition<MongoDocument<AObject>, AObjectNameProjectionT> IProjectionBase<AObject>.ToMongoProjection(string prefix)
    //    {
    //        var concreteProjection = ToMongoProjection(prefix);
    //        var serializer = global::MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry.GetSerializer<global::MongoObject.Core.Data.MongoDocument<global::Progress.Test.AObject>>();
    //        var renderArgs = new global::MongoDB.Driver.RenderArgs<global::MongoObject.Core.Data.MongoDocument<global::Progress.Test.AObject>>(serializer, global::MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry);
    //        var rendered = concreteProjection.Render(renderArgs);
    //        return new global::MongoDB.Driver.BsonDocumentProjectionDefinition<global::MongoObject.Core.Data.MongoDocument<global::Progress.Test.AObject>, global::Progress.Test.AObjectNameProjectionT>(rendered.Document);
    //    }
    //}

    //public record AObjectOtherT : global::MongoObject.Core.Interfaces.IProjectionBase, global::MongoObject.Core.Interfaces.IProjectionBase<global::Progress.Test.AObject>
    //{
    //    public int? Age { get; set; }

    //    public global::MongoDB.Driver.ProjectionDefinition<global::MongoObject.Core.Data.MongoDocument<global::Progress.Test.AObject>, global::Progress.Test.AObjectOther> ToMongoProjection(string prefix = "")
    //    {
    //        var builder = global::MongoDB.Driver.Builders<global::MongoObject.Core.Data.MongoDocument<global::Progress.Test.AObject>>.Projection;

    //        return builder.Expression(u => new global::Progress.Test.AObjectOther
    //        {
    //            Age = u.Document.Age,
    //        });
    //    }

    //    global::MongoDB.Driver.ProjectionDefinition<global::MongoObject.Core.Data.MongoDocument<global::Progress.Test.AObject>, global::MongoObject.Core.Interfaces.IProjectionBase<global::Progress.Test.AObject>> global::MongoObject.Core.Interfaces.IProjectionBase<global::Progress.Test.AObject>.ToMongoProjection(string prefix)
    //    {
    //        var concreteProjection = ToMongoProjection(prefix);
    //        var serializer = global::MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry.GetSerializer<global::MongoObject.Core.Data.MongoDocument<global::Progress.Test.AObject>>();
    //        var renderArgs = new global::MongoDB.Driver.RenderArgs<global::MongoObject.Core.Data.MongoDocument<global::Progress.Test.AObject>>(serializer, global::MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry);
    //        var rendered = concreteProjection.Render(renderArgs);
    //        return new global::MongoDB.Driver.BsonDocumentProjectionDefinition<global::MongoObject.Core.Data.MongoDocument<global::Progress.Test.AObject>, global::MongoObject.Core.Interfaces.IProjectionBase<global::Progress.Test.AObject>>(rendered.Document);
    //    }
    //}
}
