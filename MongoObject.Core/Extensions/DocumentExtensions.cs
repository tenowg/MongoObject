using MongoObject.Core.Interfaces;

namespace MongoObject.Core.Extensions
{
    public static class DocumentExtensions
    {
        // The compiler knows TMeta based on the document passed in!
        public static TMeta CreateMetadata<TDoc, TMeta>(this TDoc document, Action<TMeta>? configure = null)
            where TDoc : class, IDocumentFile, new()
            where TMeta : class, IMetadataBase, new()
        {
            var meta = new TMeta();

            // Auto-fill things the developer shouldn't have to worry about
            meta.LastModifiedAt = DateTime.UtcNow;
            meta.CreatedAt = DateTime.UtcNow;

            // Let the developer configure the rest
            configure?.Invoke(meta);

            return meta;
        }
    }
}
