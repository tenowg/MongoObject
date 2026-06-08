using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

public enum ProjectionType
{
    Include = 0,
    Exclude = 1,
    Slice = 2,
    Vector = 3,
    AutoVector = 4
}

namespace MongoObject.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ProjectValue(string ProjectionName, ProjectionType projection) : Attribute
    {
        public string ProjectionName { get; init; } = ProjectionName;
        public ProjectionType ProjectionType { get; init; } = projection;
        public int Dimensions { get; init; } = 1024;
        public VectorSimilarity Similarity { get; init; } = VectorSimilarity.Cosine;
        public string VectorModel { get; set; } = "voyage-4";
    }
}
