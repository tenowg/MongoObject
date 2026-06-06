using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.Core.Attributes
{
    public enum IndexType
    {
        Ascending = 0,
        Descending = 1,
        Vector = 2,
        Search = 3
    };

    /// <summary>
    /// MongoIndex attribute is used to mark a property as an index in MongoDB. It allows you to specify the index name, type (ascending or descending), description, and whether the index is unique.
    /// If two properties have the same index name, they will be combined into a compound index.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class MongoIndexAttribute(string IndexName) : Attribute
    {
        public string IndexName { get; set; } = IndexName;
        public IndexType Type { get; set; } = IndexType.Ascending;
        public string Description { get; set; } = string.Empty;
        public bool Unique { get; set; } = false;
    }
}
