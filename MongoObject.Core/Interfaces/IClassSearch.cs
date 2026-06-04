using MongoDB.Driver;
using MongoObject.Core.Data;

namespace MongoObject.Core.Interfaces
{
    public interface IClassSearch<T> : IClassSearch 
        where T : class, IDocumentFile, new()
    {
        public FilterDefinition<MongoDocument<T>> ToMongoFilter(string prefix = "Document");
    }

    public interface IClassSearch
    {
        
    }
}
