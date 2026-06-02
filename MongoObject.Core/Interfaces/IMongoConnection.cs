using MongoDB.Bson;
using MongoDB.Driver;
using MongoObject.Core.Data;

namespace MongoObject.Core.Interfaces
{
    public interface IMongoConnection
    {
        string CollectionName { get; }
        string DatabaseName { get; }
        void OnChanged(BsonDocument document);
    }

    public interface IMongoConnection<T> : IDisposable, IMongoConnection
        where T : class, IDocumentFile, new()
    {
        IMongoCollection<MongoDocument<T>> Collection { get; }
    }
}
