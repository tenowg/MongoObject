using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.SourceGenerator.Models
{
    internal sealed record EncryptedModel
    {
        public IReadOnlyList<PropertyModel> Properties { get; set; } = [];
    }
}
