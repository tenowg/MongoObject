using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;
using System.Runtime.CompilerServices;

namespace MongoObject.Core.Services
{
    internal class MongoDocumentKeyManager : IDocumentKeyManager
    {
        private ConditionalWeakTable<object, string> _entityTracker = [];

        public string SetKey<T>(MongoDocument<T> value) where T : class, IDocumentFile, new()
        {
            if (value.Document is null) throw new InvalidOperationException("MongoDocument.Document cannot be null to store its key");
            _entityTracker.AddOrUpdate(value.Document, value.Id);

            return value.Id;
        }

        public bool TryGetKey<T>(T document, out string? key) where T : class, IDocumentFile, new()
        {
            return _entityTracker.TryGetValue(document, out key);
        }
    }
}
