using Microsoft.Extensions.Primitives;
using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;

namespace MongoObject.Core.Services
{
    internal class DocumentMonitor<TDocument>(
            MongoDocumentManager<TDocument> documentManager, 
            IDocumentTokenChangeMonitor<TDocument> monitor,
            DistributedLockManager lockManager) : IDocumentMonitor<TDocument>, IDocumentMonitorInternal<TDocument> 
        where TDocument : class, IDocumentFile, new()
    {
        public async Task<IMongoLockScope> LockDocument(TDocument Document)
        {
            return await lockManager.LockScopedAsync(Document);
        }

        public Task Delete(TDocument document)
        {
            throw new NotImplementedException();
        }

        public async Task<TDocument> Get(string id)
        {
            return new TDocument();
        }

        public string GetKey(TDocument document)
        {
            var key = documentManager.GetKey(document);
            return key ?? string.Empty;
        }

        public async Task<SaveChangesResult> SaveChanges(TDocument document, IMongoLockScope? lockKey = null)
        {
            return await documentManager.UpdateDocument(document, lockKey);
        }

        public async Task<string> Add<TMetaSearch>(TDocument document, Action<TMetaSearch>? metadata)
            where TMetaSearch : class, IMetadataBase, new()
        {
            if (document is TDocument doc)
            {
                return await documentManager.AddDocument(doc, metadata);
            }
            return string.Empty;
        }
        public Task<string> Add(TDocument document)
        {
            throw new NotImplementedException();
        }

        public Task SaveChanges<TMetaSearch>(TDocument document, Action<TMetaSearch> metadata)
            where TMetaSearch : class, IMetadataSearchBase, new()
        {
            throw new NotImplementedException();
        }

        public Task SaveMetadata<TMetaRecord>(TDocument document, Action<TMetaRecord> metadata)
            where TMetaRecord : class, IMetadataBase, new()
        {
            throw new NotImplementedException();
        }

        public IDisposable OnChange(TDocument document, Action action)
        {
            var key = GetKey(document);
            return ChangeToken.OnChange(
                () => monitor.GetChangeToken(key),
                action);
        }
        public void Change(TDocument doc)
        {
            var key = GetKey(doc);
            monitor.SignalChange(key);
        }

        async Task<IEnumerable<TDocument>> IDocumentMonitorInternal<TDocument>.DocumentSearch<TClassSearch>(Action<TClassSearch> metadata)
        {
            return await documentManager.ClassSearch(metadata);
        }

        async Task<IEnumerable<TDocument>> IDocumentMonitorInternal<TDocument>.MetadataSearch<TMetaSearch>(Action<TMetaSearch> metadata)
        {
            return await documentManager.MetadataSearch(metadata);
        }

        async Task<IEnumerable<TDocument>> IDocumentMonitorInternal<TDocument>.CombinedSearch<TClassSearch, TMetaSearch>(
            Action<TClassSearch>? query, Action<TMetaSearch>? meta, int limit, int skip)
        {
            return await documentManager.CombinedSearch<TClassSearch, TMetaSearch>(query, meta, limit, skip);
        }

        async Task<IEnumerable<TProjection>> IDocumentMonitorInternal<TDocument>.SearchWithProjection<TClassSearch, TMetaSearch, TProjection>(
            Action<TClassSearch>? query, Action<TMetaSearch>? meta, TProjection projection, int limit, int skip)
        {
            return await documentManager.SearchWithProjection<TClassSearch, TMetaSearch, TProjection>(query, meta, projection, limit, skip);
        }

        async Task<long> IDocumentMonitorInternal<TDocument>.DeleteMany<TClassSearch, TMetaSearch>(
            Action<TClassSearch>? query, Action<TMetaSearch>? meta)
        {
            return await documentManager.DeleteMany<TClassSearch, TMetaSearch>(query, meta);
        }
    }
}
