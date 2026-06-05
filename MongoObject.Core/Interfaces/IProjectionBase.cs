using MongoDB.Driver;
using MongoObject.Core.Data;

namespace MongoObject.Core.Interfaces
{
    public interface IProjectionBase
    {
        
    }

    public interface IProjectionBase<T, U> : IProjectionBase where T : class, IDocumentFile, new () where U : class, IProjectionBase<T, U>, new ()
    {
        ProjectionDefinition<MongoDocument<T>, U> ToMongoProjection(string prefix = "");
        void SetSliceProjection(string propertyName, ProjectionVal.Slice slice);
    }
}
