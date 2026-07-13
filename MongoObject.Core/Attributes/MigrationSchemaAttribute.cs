namespace MongoObject.Core.Attributes
{
    public enum OrphanFieldPolicy
    {
        AlwaysAsk,
        Ignore,   
        Warn, // default
        Delete
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class MigrationSchemaAttribute(OrphanFieldPolicy policy) : Attribute
    {
        public OrphanFieldPolicy Policy { get; init; } = policy;
    }
}