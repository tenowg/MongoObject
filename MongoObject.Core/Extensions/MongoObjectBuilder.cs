using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;
using MongoObject.Core.Services;
using System.Diagnostics.CodeAnalysis;

namespace MongoObject.Core.Extensions
{
    public static class MongoObjectsPluginRegistry
    {
        public static List<Action<IServiceCollection, IConfiguration>> RegisterDocumentsHook { get; } = [];
        public static BsonDocument SchemaDocument { get; } = [];
        private static readonly Dictionary<Type, Func<IMongoDatabase, string, MigrationOperation, Task>> _handlers = new();

        public static void RegisterHandler<TOp>(Func<IMongoDatabase, string, TOp, Task> handler) 
        where TOp : MigrationOperation
        {
            _handlers[typeof(TOp)] = (db, coll, op) => handler(db, coll, (TOp)op);
        }

        public static Task ExecuteAsync(Type opType, IMongoDatabase db, string coll, MigrationOperation op)
        {
            if (!_handlers.TryGetValue(opType, out var handler))
                throw new NotSupportedException($"No handler registered for {opType.Name}. Is the extension package missing?");
            
            return handler(db, coll, op);
        }
    }

    public class MongoObjectBuilder(IServiceCollection sp)
    {
        public IServiceCollection Services => sp;
        public MongoObjectBuilder RegisterDocument<TDocument, TMetaSearch, TMetaRecord>(bool IsSecured) 
            where TDocument : class, IDocumentFile<TMetaSearch, TMetaRecord>, new()
            where TMetaSearch : class, IMetadataSearchBase, new()
            where TMetaRecord : class, IMetadataBase, new()
        {
            sp.AddSingleton<IDocumentTokenChangeMonitor<TDocument>, DocumentChangeTokenMonitor<TDocument>>();
            sp.AddSingleton<IDocumentMonitor<TDocument>, DocumentMonitor<TDocument>>();
            // this is what the code gened code needs to replace
            sp.AddSingleton<IMongoConnection<TDocument>, MongoConnection<TDocument>>();
            sp.AddSingleton<IMongoConnection, MongoConnection<TDocument>>();
            sp.AddSingleton<MongoDocumentManager<TDocument>>();
            return this;
        }

        public MongoObjectBuilder RegisterIndexBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TIndexBuilder>()
            where TIndexBuilder : class, IIndexBuilder, new()
        {
            sp.AddSingleton<IIndexBuilder, TIndexBuilder>();
            return this;
        }

        public MongoObjectBuilder AddWatchStream()
        {
            sp.AddHostedService<MongoDocumentWatcherStream>();
            return this;
        }

        public MongoObjectBuilder AddWatchPolling()
        {
            sp.AddHostedService<MongoDocumentWatcherPolling>();
            return this;
        }
    }
}
