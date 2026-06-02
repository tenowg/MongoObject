namespace MongoObject.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class PropertyNameChangeAttribute(string migrationKey, string oldName, string newName) : Attribute
    {
        public string MigrationKey { get; } = migrationKey;
        public string OldName { get; } = oldName;
        public string NewName { get; } = newName;
    }
}
