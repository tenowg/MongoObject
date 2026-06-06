using Microsoft.Extensions.DependencyInjection;
using MongoObject.Core.Interfaces;
using MongoObject.Core.Services;
using System.Diagnostics.CodeAnalysis;

namespace MongoObject.Core.Extensions
{
    public partial class MongoObjectBuilder(IServiceCollection sp)
    {
        public MongoObjectBuilder RegisterDocument<TDocument, TMetaSearch, TMetaRecord>() 
            where TDocument : class, IDocumentFile<TMetaSearch, TMetaRecord>, new()
            where TMetaSearch : class, IMetadataSearchBase, new()
            where TMetaRecord : class, IMetadataBase, new()
        {
            sp.AddSingleton<IDocumentTokenChangeMonitor<TDocument>, DocumentChangeTokenMonitor<TDocument>>();
            //sp.AddSingleton<IDocumentMonitor<TDocument, TMeta>, DocumentMonitor<TDocument, TMeta>>();
            sp.AddSingleton<IDocumentMonitor<TDocument>, DocumentMonitor<TDocument>>();
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
