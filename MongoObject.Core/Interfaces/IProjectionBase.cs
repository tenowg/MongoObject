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

    public interface IProjectionBase<T, U> : IProjectionBase where T : class, IDocumentFile, new () where U : class, IProjectionBase<T, U>, new ()
    {
        ProjectionDefinition<MongoDocument<T>, U> ToMongoProjection(string prefix = "");
    }
}
