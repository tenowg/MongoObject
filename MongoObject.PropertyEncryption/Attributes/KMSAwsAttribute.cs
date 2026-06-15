using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.PropertyEncryption.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class KMSAwsAttribute : Attribute
    {
        public string Key { get; set; } = string.Empty;
        public string? AccessKeyPath { get; set; }
        public string? SecretKeyPath { get; set; }
        public string? SessionTokenPath { get; set; } // Optional token field
    }
}
