using MongoObject.Core.Data;

namespace MongoObject.Core.Interfaces
{
    public interface IDocumentMonitor<TDocument> where TDocument : class, IDocumentFile, new()
    {
        Task<IMongoLockScope> LockDocument(TDocument Document);
        internal Task<string> Add(TDocument document);
        void Change(TDocument doc);
        Task Delete(TDocument document);
        Task<TDocument> Get(string id);
        string GetKey(TDocument document);
        IDisposable OnChange(TDocument document, Action action);
        Task<SaveChangesResult> SaveChanges(TDocument document, IMongoLockScope? lockKey = null);
    }

    public interface IDocumentMonitorInternal<TDocument> where TDocument : class, IDocumentFile, new()
    {
        Task<string> Add<TMetaSearch>(TDocument document, Action<TMetaSearch>? metadata) where TMetaSearch : class, IMetadataBase, new();
        Task<IEnumerable<TDocument>> DocumentSearch<TClassSearch>(Action<TClassSearch> metadata) where TClassSearch : class, IClassSearch<TDocument>, new();
        Task<IEnumerable<TDocument>> MetadataSearch<TMetaSearch>(Action<TMetaSearch> metadata) where TMetaSearch : class, IMetadataSearchBase, new();
        
        // Builder support methods
        Task<IEnumerable<TDocument>> CombinedSearch<TClassSearch, TMetaSearch>(Action<TClassSearch>? query, Action<TMetaSearch>? meta, int limit = 0, int skip = 0) 
            where TClassSearch : class, IClassSearch<TDocument>, new()
            where TMetaSearch : class, IMetadataSearchBase, new();
        
        Task<IEnumerable<TProjection>> SearchWithProjection<TClassSearch, TMetaSearch, TProjection>(
            Action<TClassSearch>? query, Action<TMetaSearch>? meta, int limit = 0, int skip = 0) 
            where TClassSearch : class, IClassSearch<TDocument>, new()
            where TMetaSearch : class, IMetadataSearchBase, new()
            where TProjection : class, IProjectionBase<TDocument, TProjection>, new();
        
        Task<long> DeleteMany<TClassSearch, TMetaSearch>(Action<TClassSearch>? query, Action<TMetaSearch>? meta)
            where TClassSearch : class, IClassSearch<TDocument>, new()
            where TMetaSearch : class, IMetadataSearchBase, new();
    }
}
