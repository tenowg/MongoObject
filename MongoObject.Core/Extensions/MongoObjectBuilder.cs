using Microsoft.Extensions.DependencyInjection;
using MongoObject.Core.Interfaces;
using MongoObject.Core.Services;

namespace MongoObject.Core.Extensions
{
    public class MongoObjectBuilder(IServiceCollection sp)
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

        public MongoObjectBuilder AddWatchStream()
        {
            sp.AddHostedService<MongoDocumentWatcher>();
            return this;
        }

        public MongoObjectBuilder AddWatchPolling()
        {
            return this;
        }
    }
}
