using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.SourceGenerator.Encryption.Models
{
    internal sealed record AzureModel
    {
        public string? Key { get; set; }
        public string? TenantIdPath { get; set; }
        public string? ClientIdPath { get; set; }
        public string? ClientSecretPath { get; set; }
    }
}
