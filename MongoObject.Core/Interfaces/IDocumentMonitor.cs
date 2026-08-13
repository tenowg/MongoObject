using MongoDB.Driver;
using MongoObject.Core.Data;
using System.Linq.Expressions;

namespace MongoObject.Core.Interfaces
{
    public interface IDocumentMonitor<TDocument> where TDocument : class, IDocumentFile, new()
    {
        IMongoCollection<MongoDocument<TDocument>> GetConnection();
        Task<IMongoLockScope> LockDocument(TDocument Document, CancellationToken cancellationToken = default);
        void Change(TDocument doc);
        Task<DeleteResult> Delete(TDocument document, CancellationToken cancellationToken = default);
        Task<TDocument?> Get(string id, CancellationToken cancellationToken = default);
        string GetKey(TDocument document);
        IDisposable OnChange(TDocument document, Action action);
        Task<SaveChangesResult> SaveChanges(TDocument document, IMongoLockScope? lockKey = null, CancellationToken cancellationToken = default);
    }

    public interface IDocumentMonitorInternal<TDocument> where TDocument : class, IDocumentFile, new()
    {
        Task<string> Add<TMetaSearch>(TDocument document, Action<TMetaSearch>? metadata, CancellationToken cancellationToken = default) where TMetaSearch : class, IMetadataBase, new();
        Task<IEnumerable<TDocument>> DocumentSearch<TClassSearch>(Action<TClassSearch> metadata, CancellationToken cancellationToken = default) where TClassSearch : class, IClassSearch<TDocument>, new();
        Task<IEnumerable<TDocument>> MetadataSearch<TMetaSearch>(Action<TMetaSearch> metadata, CancellationToken cancellationToken = default) where TMetaSearch : class, IMetadataSearchBase, new();
        
        // Builder support methods
        Task<IEnumerable<TDocument>> CombinedSearch<TClassSearch, TMetaSearch>(Action<TClassSearch>? query, Action<TMetaSearch>? meta, SortDefinition<MongoDocument<TDocument>> sort, int limit = 0, int skip = 0, CancellationToken cancellationToken = default) 
            where TClassSearch : class, IClassSearch<TDocument>, new()
            where TMetaSearch : class, IMetadataSearchBase, new();
        
        Task<IEnumerable<TProjection>> SearchWithProjection<TClassSearch, TMetaSearch, TProjection>(
            Action<TClassSearch>? query, Action<TMetaSearch>? meta, TProjection projection, SortDefinition<MongoDocument<TDocument>> sort, int limit = 0, int skip = 0, CancellationToken cancellationToken = default) 
            where TClassSearch : class, IClassSearch<TDocument>, new()
            where TMetaSearch : class, IMetadataSearchBase, new()
            where TProjection : class, IProjectionBase<TDocument, TProjection>, new();
        
        Task<long> DeleteMany<TClassSearch, TMetaSearch>(Action<TClassSearch>? query, Action<TMetaSearch>? meta, CancellationToken cancellationToken = default)
            where TClassSearch : class, IClassSearch<TDocument>, new()
            where TMetaSearch : class, IMetadataSearchBase, new();

        Task<IEnumerable<TProjection>> VectorSearchWithProjection<TClassSearch, TMetaSearch, TProjection>(
            Action<TClassSearch>? query, Action<TMetaSearch>? meta, TProjection projection, string index, string embeddingName, float[] embedding, int limit = 0, int skip = 0, int returnCount = -1, int conciderFrom = 150, CancellationToken cancellationToken = default)
            where TClassSearch : class, IClassSearch<TDocument>, new()
            where TMetaSearch : class, IMetadataSearchBase, new()
            where TProjection : class, IProjectionBase<TDocument, TProjection>, new();

        Task<IEnumerable<TProjection>> AutoVectorSearchWithProjection<TClassSearch, TMetaSearch, TProjection, TField>(
            Action<TClassSearch>? query, Action<TMetaSearch>? meta, TProjection projection, string index, Expression<Func<MongoDocument<TDocument>, TField>> embeddingName,  string embedding, int limit = 0, int skip = 0, int returnCount = -1, int conciderFrom = 150, CancellationToken cancellationToken = default)
            where TClassSearch : class, IClassSearch<TDocument>, new()
            where TMetaSearch : class, IMetadataSearchBase, new()
            where TProjection : class, IProjectionBase<TDocument, TProjection>, new();
    }
}
