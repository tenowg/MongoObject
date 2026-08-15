using Microsoft.Extensions.Primitives;
using MongoDB.Driver;
using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;
using System.Linq.Expressions;

namespace MongoObject.Core.Services
{
    internal class DocumentMonitor<TDocument>(
            MongoDocumentManager<TDocument> documentManager, 
            IMongoConnection<TDocument> connection,
            IDocumentTokenChangeMonitor<TDocument> monitor,
            IDistributedLockManager lockManager) : IDocumentMonitor<TDocument>, IDocumentMonitorInternal<TDocument> 
        where TDocument : class, IDocumentFile, new()
    {
        public IMongoCollection<MongoDocument<TDocument>> GetConnection() => connection.Collection;
        public async Task<IMongoLockScope> LockDocument(TDocument Document, CancellationToken cancellationToken = default)
        {
            return await lockManager.LockScopedAsync(Document, null, cancellationToken);
        }

        public Task<DeleteResult> Delete(TDocument document, CancellationToken cancellationToken = default)
        {
            return documentManager.DeleteDocument(document, cancellationToken);
        }

        public async Task<TDocument?> Get(string id, CancellationToken cancellationToken = default)
        {
            return await documentManager.GetDocument<NoOpSearchBase>(id, null, cancellationToken);
        }

        public string GetKey(TDocument document)
        {
            var key = documentManager.GetKey(document);
            return key ?? string.Empty;
        }

        public async Task<SaveChangesResult> SaveChanges(TDocument document, IMongoLockScope? lockKey = null, CancellationToken cancellationToken = default)
        {
            return await documentManager.UpdateDocument(document, lockKey, cancellationToken);
        }

        public async Task<string> Add<TMetaSearch>(TDocument document, Action<TMetaSearch>? metadata = null, CancellationToken cancellationToken = default)
            where TMetaSearch : class, IMetadataBase, new()
        {
            if (document is TDocument doc)
            {
                return await documentManager.AddDocument(doc, metadata, cancellationToken);
            }
            return string.Empty;
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

        async Task<IEnumerable<TDocument>> IDocumentMonitorInternal<TDocument>.DocumentSearch<TClassSearch>(Action<TClassSearch> metadata, CancellationToken cancellationToken)
        {
            return await documentManager.ClassSearch(metadata, cancellationToken);
        }

        async Task<IEnumerable<TDocument>> IDocumentMonitorInternal<TDocument>.MetadataSearch<TMetaSearch>(Action<TMetaSearch> metadata, CancellationToken cancellationToken)
        {
            return await documentManager.MetadataSearch(metadata, cancellationToken);
        }

        async Task<IEnumerable<TDocument>> IDocumentMonitorInternal<TDocument>.CombinedSearch<TClassSearch, TMetaSearch>(
            Action<TClassSearch>? query, Action<TMetaSearch>? meta, SortDefinition<MongoDocument<TDocument>> sort, int limit, int skip, CancellationToken cancellationToken)
        {
            return await documentManager.CombinedSearch<TClassSearch, TMetaSearch>(query, meta, sort, limit, skip);
        }

        async Task<IEnumerable<TProjection>> IDocumentMonitorInternal<TDocument>.SearchWithProjection<TClassSearch, TMetaSearch, TProjection>(
            Action<TClassSearch>? query, Action<TMetaSearch>? meta, TProjection projection, SortDefinition<MongoDocument<TDocument>> sort, int limit, int skip, CancellationToken cancellationToken)
        {
            return await documentManager.SearchWithProjection<TClassSearch, TMetaSearch, TProjection>(query, meta, projection, sort, limit, skip, cancellationToken);
        }

        async Task<long> IDocumentMonitorInternal<TDocument>.DeleteMany<TClassSearch, TMetaSearch>(
            Action<TClassSearch>? query, Action<TMetaSearch>? meta, CancellationToken cancellationToken)
        {
            return await documentManager.DeleteMany<TClassSearch, TMetaSearch>(query, meta, cancellationToken);
        }

        async Task<IEnumerable<TProjection>> IDocumentMonitorInternal<TDocument>.VectorSearchWithProjection<TClassSearch, TMetaSearch, TProjection>(Action<TClassSearch>? query, Action<TMetaSearch>? meta, TProjection projection, string index, string embeddingName, float[] embedding, int limit, int skip, int returnCount, int conciderFrom, CancellationToken cancellationToken)
        {
            return await documentManager.SearchWithVector(query, meta, projection, index, embeddingName, embedding, limit, skip, returnCount, conciderFrom, cancellationToken);
        }

        async Task<IEnumerable<TProjection>> IDocumentMonitorInternal<TDocument>.AutoVectorSearchWithProjection<TClassSearch, TMetaSearch, TProjection, TField>(
            Action<TClassSearch>? query, 
            Action<TMetaSearch>? meta, 
            TProjection projection, 
            string index,
            Expression<Func<MongoDocument<TDocument>, TField>> embeddingName, 
            string embedding, 
            int limit, 
            int skip, 
            int returnCount, 
            int conciderFrom,
            CancellationToken cancellationToken)
        {
            return await documentManager.SearchWithAutoVector(query, meta, projection, index, embeddingName, embedding, limit, skip, returnCount, conciderFrom, cancellationToken);
        }

        async Task<TDocument?> IDocumentMonitorInternal<TDocument>.GetById<TMetaSearch>(string id, Action<TMetaSearch>? meta, CancellationToken cancellationToken)
        {
            return await documentManager.GetDocument(id, meta, cancellationToken);
        }
    }
}
