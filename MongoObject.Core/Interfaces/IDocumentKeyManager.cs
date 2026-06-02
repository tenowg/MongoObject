using MongoObject.Core.Data;

namespace MongoObject.Core.Interfaces
{
    public interface IDocumentKeyManager
    {
        string SetKey<T>(MongoDocument<T> value) where T : class, IDocumentFile, new();
        bool TryGetKey<T>(T document, out string? key) where T : class, IDocumentFile, new();
    }
}