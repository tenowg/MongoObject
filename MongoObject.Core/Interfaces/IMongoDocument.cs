using MongoDB.Bson;

namespace MongoObject.Core.Interfaces
{
    public interface IMongoDocument<T> where T : class, IDocumentFile, new()
    {
        public string Id { get; set; }
        public T? Document { get; set; }
        public BsonDocument Metadata { get; set; }
    }
}
