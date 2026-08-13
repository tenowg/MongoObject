using MongoObject.Core.Data;
using System.Diagnostics.CodeAnalysis;

namespace MongoObject.Core.Interfaces
{
    public interface IDistributedLockManager
    {
        Task<LockMetadata?> GetLock(IMongoLockScope recordKey);
        Task<LockMetadata?> GetLock(string key);
        Task<bool> IsLockedByOther<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(IMongoLockScope? scope, T document) where T : class, IDocumentFile, new();
        Task<LockAcquisitionResult> LockDocumentAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(T document, TimeSpan? duration = null) where T : class, IDocumentFile, new();
        Task<LockAcquisitionResult> LockDocumentAsyncNew<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(T document, TimeSpan? duration = null) where T : class, IDocumentFile, new();
        Task<IMongoLockScope> LockScopedAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(T document, TimeSpan? duration = null, CancellationToken cancellationToken = default) where T : class, IDocumentFile, new();
        Task ReleaseLockAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string recordKey, string holderId) where T : class, IDocumentFile, new();
        Task ReleaseLockAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(T document, string holderId) where T : class, IDocumentFile, new();
        Task<bool> RenewLockAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string recordKey, string holderId, TimeSpan? extendBy = null) where T : class, IDocumentFile, new();
    }
}