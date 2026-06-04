#region ExampleClass
using MongoObject.Core.Attributes;

namespace MongoObject.Examples.Models
{
    [MongoObject]
    public partial class ExampleClass
    {
        public partial string? Name { get; set; } = string.Empty;
        public partial int? Age { get; set; }
    }
}
#endregion