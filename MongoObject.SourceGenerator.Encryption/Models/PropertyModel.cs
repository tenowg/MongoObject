using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.SourceGenerator.Encryption.Models
{
    internal sealed record PropertyModel
    {
        public string Name { get; init; } = string.Empty;
        public LocalModel? Local { get; init; }
        public AwsModel? Aws { get; init; }
        public AzureModel? Azure { get; init; }
    }
}
