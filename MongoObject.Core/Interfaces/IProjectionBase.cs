using MongoDB.Driver;
using MongoObject.Core.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.Core.Interfaces
{
    public interface IProjectionBase
    {
        
    }

    public interface IProjectionBase<T> : IProjectionBase where T : class, IDocumentFile, new ()
    {
        ProjectionDefinition<MongoDocument<T>> ToMongoProjection(string prefix = "");
    }
}
