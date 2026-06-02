using MongoDB.Driver;
using MongoObject.Core.Data;

namespace MongoObject.Core.Interfaces
{
    public interface IDocumentFile<TMetaSearch, TMetaRecord> : IDocumentFile
        where TMetaSearch : class, IMetadataSearchBase, new()
        where TMetaRecord : class, IMetadataBase, new() 
    {
       
    }

    public interface IDocumentFile
    {
        Type GetSearchMetaType();
        Type GetRecordMetaType();
        
        string GetDatabaseName();
        string GetCollectioName();
    }

    public interface IDocumentFileInternal
    {
        void TrackChanges();
        UpdateDefinition<MongoDocument<T>> GetPendingUpdates<T>()
            where T : class, IDocumentFile, new();
        void ClearChanges();
        public bool TryGetPendingUpdatesPipeline<T>(out UpdateDefinition<MongoDocument<T>>? update) where T : class, IDocumentFile, new();
    }
}
