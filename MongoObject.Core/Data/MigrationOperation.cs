using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoObject.Core.Extensions;

namespace MongoObject.Core.Data
{
    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(
        typeof(ApplyValidationSchemaOperation),
        typeof(RenamePropertyOperation), 
        typeof(DeletePropertyOperation),
        typeof(DeleteCollectionOperation),
        typeof(RenameCollectionOperation),
        typeof(DisableValidation),
        typeof(CreateCollectionOperation))] 
    public abstract record MigrationOperation
    {
        public bool RequiresEnc { get; init; } = false;
    };

    public sealed record ApplyValidationSchemaOperation(
        [property: BsonElement("Schema")] BsonDocument Schema
    ) : MigrationOperation;

    public sealed record RenamePropertyOperation(
        string From,
        string To
    ) : MigrationOperation;

    public sealed record DeletePropertyOperation(
        string Property
    ) : MigrationOperation;

    public sealed record DeleteCollectionOperation()
        : MigrationOperation;

    public sealed record RenameCollectionOperation(
        string NewName
    ) : MigrationOperation;

    public sealed record CreateCollectionOperation(
        BsonDocument Schema
    ) : MigrationOperation;

    public sealed record DisableValidation : MigrationOperation;

    public readonly record struct MongoNamespace(string Database, string Collection)
    {
        public static MongoNamespace Parse(string ns) 
        {
            var parts = ns.Split('.');
            return new MongoNamespace(parts[0], parts[1]);
        }
    }

    public static class RegisterOperations
    {
        public static void Register()
        {
            MongoObjectsPluginRegistry.RegisterHandler<DisableValidation>(async (db, coll, op) =>
            {
                Console.WriteLine("DisableValidation");
                var command = new BsonDocument
                {
                    { "collMod", coll },
                    { "validationLevel", "off" }
                };

                try
                {
                    db.RunCommand<BsonDocument>(command);
                    Console.WriteLine($"Validator successfully disabled on {coll}.");
                }
                catch (MongoCommandException ex)
                {
                    Console.WriteLine($"Error disabling validator: {ex.Message}");
                }
            });

            MongoObjectsPluginRegistry.RegisterHandler<CreateCollectionOperation>(async (db, coll, op) =>
            {
                Console.WriteLine("Creating Collection");
                var options = new CreateCollectionOptions<object>
                {
                    Validator = op.Schema,
                    ValidationAction = DocumentValidationAction.Error,
                    ValidationLevel = DocumentValidationLevel.Strict
                };

                await db.CreateCollectionAsync(coll, options);

            });
            MongoObjectsPluginRegistry.RegisterHandler<ApplyValidationSchemaOperation>(async (db, coll, op) =>
            {
                Console.WriteLine("Applying Validation");
                var command = new BsonDocument
                {
                    { "collMod", coll },
                    { "validator", op.Schema.AsBsonDocument },
                    { "validationLevel", "strict" },
                    { "validationAction", "error" }
                };

                try
                {
                    db.RunCommand<BsonDocument>(command);
                    Console.WriteLine("Validator successfully applied to existing collection.");
                }
                catch (MongoCommandException ex)
                {
                    Console.WriteLine($"Error applying validator: {ex.Message}");
                }
            });

            MongoObjectsPluginRegistry.RegisterHandler<RenamePropertyOperation>(async (db, coll, op) =>
            {
                Console.WriteLine($"Renaming Property {op.From} to {op.To}");
                var collection = db.GetCollection<BsonDocument>(coll);
                var rename = new BsonDocument("$rename",
                new BsonDocument(op.From, op.To));

                await collection.UpdateManyAsync(
                    FilterDefinition<BsonDocument>.Empty,
                    rename);
            });

            MongoObjectsPluginRegistry.RegisterHandler<DeletePropertyOperation>(async (db, coll, op) =>
            {
                Console.WriteLine($"Deleting Property {op.Property}.");
                var collection = db.GetCollection<BsonDocument>(coll);
                var filter = Builders<BsonDocument>.Filter.Empty;
                var update = Builders<BsonDocument>.Update.Unset(op.Property);

                await collection.UpdateManyAsync(filter, update);
            });
        }
    }
}