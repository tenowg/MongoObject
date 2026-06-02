using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Text;

public enum ProjectionType
{
    Include,
    Exclude
}

namespace MongoObject.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ProjectValue(string ProjectionName, ProjectionType projection) : Attribute
    {
        public string ProjectionName { get; init; } = ProjectionName;
        public ProjectionType ProjectionType { get; init; } = projection;
    }
}
