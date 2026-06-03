using MongoObject.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.Test.Dummies
{
    [MongoObject]
    internal class DummyObject
    {
        public partial string DummyString { get; set; }
        public partial int DummyInt { get; set; }
        public partial DateTime DummyDate { get; set; }
    }
}
