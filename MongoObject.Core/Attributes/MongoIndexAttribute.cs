namespace MongoObject.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MongoIndexAttribute(string Name) : Attribute
    {
        public string Name { get; set; } = Name;
        public bool Unique { get; set; } = false;
    }
}