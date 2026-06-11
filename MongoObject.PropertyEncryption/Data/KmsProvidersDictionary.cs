using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.PropertyEncryption.Data
{
    public class KmsProvidersDictionary : Dictionary<string, IReadOnlyDictionary<string, object>>
    {
    }
}
