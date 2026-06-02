namespace MongoObject.Core.Interfaces
{
    public interface IMongoLockScope : IAsyncDisposable
    {
        string HolderId { get; }
    }
}
