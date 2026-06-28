using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoObject.Core.Data
{
    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(
        typeof(ApplyValidationSchemaOperation),
        typeof(RenamePropertyOperation), 
        typeof(DeletePropertyOperation),
        typeof(DeleteCollectionOperation),
        typeof(RenameCollectionOperation),
        typeof(CreateCollectionOperation))] 
    public abstract record MigrationOperation
    {
        
    }

    public sealed record ApplyValidationSchemaOperation(
        BsonDocument Schema
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

    public readonly record struct MongoNamespace(string Database, string Collection)
    {
        public static MongoNamespace Parse(string ns) 
        {
            var parts = ns.Split('.');
            return new MongoNamespace(parts[0], parts[1]);
        }
    }
}