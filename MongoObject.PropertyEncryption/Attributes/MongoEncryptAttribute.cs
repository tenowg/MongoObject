namespace MongoObject.PropertyEncryption.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MongoEncryptAttribute(string Key) : Attribute
    {
        public string Key { get; set; } = Key;
    }
}
