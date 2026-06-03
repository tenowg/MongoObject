using MongoObject.Core.Attributes;

namespace MongoObject.Tests.Dummies
{
    [MongoObject]
    public partial class DummyObject
    {
        [ProjectValue("DummyString", ProjectionType.Include)]
        public partial string DummyString { get; set; }
        [ProjectValue("DummyInt", ProjectionType.Include)]
        [ProjectValue("DummyCombo", ProjectionType.Include)]
        public partial int DummyInt { get; set; }
        [ProjectValue("DummyCombo", ProjectionType.Include)]
        public partial DateTime DummyDate { get; set; }
        public partial DummyNestedObject DummyNestedObject { get; set; } = new();
    }

    [MongoObject]
    public partial class DummyNestedObject
    {
        public partial string NestedDummyString { get;  set; }
    }
}
