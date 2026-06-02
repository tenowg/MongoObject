using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoObject.Core.Interfaces;

namespace MongoObject.Core.Data
{
    public class MongoDocument<T> : IMongoDocument<T>
        where T : class, IDocumentFile, new()
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public T? Document { get; set; }
        public required BsonDocument Metadata { get;set; }
    }
}
