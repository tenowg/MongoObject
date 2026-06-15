using MongoDB.Bson;
using MongoDB.Driver;
using MongoObject.Core.Data;

namespace MongoObject.Core.Interfaces
{
    public interface IMongoConnection
    {
        string CollectionName { get; }
        string DatabaseName { get; }
        [Obsolete]
        void OnChanged(BsonDocument document);
        void OnChanged(string id);
        Type DocumentType();
        IMongoClient GetMongoClient();
    }

    public interface IMongoConnection<T> : IDisposable
        where T : class, IDocumentFile, new()
    {
        IMongoCollection<MongoDocument<T>> Collection { get; }
    }
}
