using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.SourceGenerator.Encryption.Models
{
    internal sealed record AwsModel
    {
        public string? Key { get; set; }
        public string? AccessKeyPath { get; set; }
        public string? SecretKeyPath { get; set; }
        public string? SessionTokenPath { get; set; }
        public bool IsOverrided => (!string.IsNullOrEmpty(AccessKeyPath) && !string.IsNullOrEmpty(SecretKeyPath)) || !string.IsNullOrEmpty(SessionTokenPath);
    }
}
