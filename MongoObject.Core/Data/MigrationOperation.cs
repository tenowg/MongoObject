using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoObject.Core.Extensions;
using SharpCompress.Compressors.ZStandard.Unsafe;

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
        typeof(CreateIndexOperation),
        typeof(DropIndexOperation),
        typeof(CreateCollectionOperation))] 
    public abstract record MigrationOperation
    {
        public bool RequiresEnc { get; init; } = false;
        public string? Reason { get; init; } = string.Empty;
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

    public sealed record DropIndexOperation(
        string IndexName
    ) : MigrationOperation;
    

    public sealed record CreateIndexOperation
    (
        string IndexName,
        Dictionary<string, string> Members,
        bool Unique
    ) : MigrationOperation;

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
            MongoObjectsPluginRegistry.RegisterHandler<DisableValidation>(async (db, coll, op, cancellationToken) =>
            {
                Console.WriteLine("DisableValidation");
                var command = new BsonDocument
                {
                    { "collMod", coll },
                    { "validationLevel", "off" }
                };

                try
                {
                    db.RunCommand<BsonDocument>(command, cancellationToken: cancellationToken);
                    Console.WriteLine($"Validator successfully disabled on {coll}.");
                }
                catch (MongoCommandException ex)
                {
                    Console.WriteLine($"Error disabling validator: {ex.Message}");
                }
            });

            MongoObjectsPluginRegistry.RegisterHandler<CreateCollectionOperation>(async (db, coll, op, cancellationToken) =>
            {
                Console.WriteLine("Creating Collection");
                var options = new CreateCollectionOptions<object>
                {
                    Validator = op.Schema,
                    ValidationAction = DocumentValidationAction.Error,
                    ValidationLevel = DocumentValidationLevel.Strict
                };

                await db.CreateCollectionAsync(coll, options, cancellationToken);

            });
            MongoObjectsPluginRegistry.RegisterHandler<ApplyValidationSchemaOperation>(async (db, coll, op, cancellationToken) =>
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
                    db.RunCommand<BsonDocument>(command, cancellationToken: cancellationToken);
                    Console.WriteLine("Validator successfully applied to existing collection.");
                }
                catch (MongoCommandException ex)
                {
                    Console.WriteLine($"Error applying validator: {ex.Message}");
                }
            });

            MongoObjectsPluginRegistry.RegisterHandler<RenamePropertyOperation>(async (db, coll, op, cancellationToken) =>
            {
                Console.WriteLine($"Renaming Property {op.From} to {op.To}");
                var collection = db.GetCollection<BsonDocument>(coll);
                var rename = new BsonDocument("$rename",
                new BsonDocument(op.From, op.To));

                await collection.UpdateManyAsync(FilterDefinition<BsonDocument>.Empty, rename, cancellationToken: cancellationToken);
            });

            MongoObjectsPluginRegistry.RegisterHandler<DeletePropertyOperation>(async (db, coll, op, cancellationToken) =>
            {
                Console.WriteLine($"Deleting Property {op.Property}.");
                var collection = db.GetCollection<BsonDocument>(coll);
                var filter = Builders<BsonDocument>.Filter.Empty;
                var update = Builders<BsonDocument>.Update.Unset(op.Property);

                await collection.UpdateManyAsync(filter, update);
            });

            MongoObjectsPluginRegistry.RegisterHandler<DropIndexOperation>(async (db, coll, op, cancellationToken) =>
            {
                var collection = db.GetCollection<BsonDocument>(coll);
                await collection.Indexes.DropOneAsync(op.IndexName, cancellationToken);
            });

            MongoObjectsPluginRegistry.RegisterHandler<CreateIndexOperation>(async (db, coll, op, cancellationToken) =>
            {
                var keyDefinitions = new List<IndexKeysDefinition<BsonDocument>>();

                var builder = Builders<BsonDocument>.IndexKeys;

                foreach(var member in op.Members)
                {
                    var definition = member.Value switch
                    {
                        "Ascending"  => builder.Ascending(member.Key),
                        "Descending" => builder.Descending(member.Key),
                        "Text"       => builder.Text(member.Key),
                        "Hashed"     => builder.Hashed(member.Key),
                        "Geo2d"      => builder.Geo2D(member.Key),
                        "Geo2dsphere"=> builder.Geo2DSphere(member.Key),
                        "Wildcard"   => builder.Wildcard(member.Key),
                        _ => null
                    };

                    if (definition != null)
                        keyDefinitions.Add(definition);
                }
                var combined = builder.Combine(keyDefinitions);

                var collection = db.GetCollection<BsonDocument>(coll);
                await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(combined, new CreateIndexOptions { Name = op.IndexName, Unique = op.Unique }), cancellationToken: cancellationToken); 
            });
        }
    }
}