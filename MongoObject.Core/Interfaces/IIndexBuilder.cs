namespace MongoObject.Core.Interfaces
{
    public interface IIndexBuilder
    {
        Task EnsureIndexExists(IServiceProvider sp);
    }
}
