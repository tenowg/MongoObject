using System;
using System.Collections.Generic;
using System.Text;
using MongoObject.Core.Attributes;

namespace MongoObject.Template
{
    public partial record MongoObjectBaseMeta
    {
        // Example property remove and add your own
        public string? Property { get; set; }
    }

    [MongoObject(MetadataType = typeof(MongoObjectBaseMeta))]
    public partial class MongoObjectBase
    {
        [MongoIndex("ExampleIndex", Type = IndexType.Ascending, Unique = true)]
        public partial string ExampleProperty { get; set; }
    }
}
