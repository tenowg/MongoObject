using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoObject.Core.Data;

namespace MongoObject.Core.Extensions
{
    public static class MongoObjectsPluginRegistry
    {
        public static List<Action<IServiceCollection, IConfiguration>> RegisterDocumentsHook { get; } = [];
        public static BsonDocument SchemaDocument { get; } = [];
        private static readonly Dictionary<Type, Func<IMongoDatabase, string, MigrationOperation, CancellationToken, Task>> _handlers = new();

        private static Func<Task>? _migrateTask;

        public static void RegisterHandler<TOp>(Func<IMongoDatabase, string, TOp, CancellationToken, Task> handler) 
        where TOp : MigrationOperation
        {
            _handlers[typeof(TOp)] = (db, coll, op, cancellationToken) => handler(db, coll, (TOp)op, cancellationToken);
        }

        public static void RegisterMigration(Func<Task> migrationTask)
        {
            _migrateTask = migrationTask;
        }

        public static async Task RunMigrations()
        {
            if (_migrateTask != null)
            {
                await _migrateTask();
            }
        }

        public static Task ExecuteAsync(Type type, IMongoDatabase db, string coll, MigrationOperation op, CancellationToken cancellationToken = default)
        {
            if (!_handlers.TryGetValue(type, out var handler))
                throw new NotSupportedException($"No handler registered for {type.Name}. Is the extension package missing?");
            
            return handler(db, coll, op, cancellationToken);
        }
    }
}
