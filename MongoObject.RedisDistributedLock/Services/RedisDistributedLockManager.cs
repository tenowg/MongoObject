using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace MongoObject.RedisDistributedLock.Services
{
    public class RedisDistributedLockManager(MongoObjectOptions options, IDocumentKeyManager keys) : IDistributedLockManager
    {
        public Task<LockMetadata?> GetLock(IMongoLockScope recordKey)
        {
            throw new NotImplementedException();
        }

        public Task<LockMetadata?> GetLock(string key)
        {
            throw new NotImplementedException();
        }

        Task<bool> IDistributedLockManager.IsLocked<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(IMongoLockScope? scope, T document)
        {
            throw new NotImplementedException();
        }

        Task<LockAcquisitionResult> IDistributedLockManager.LockDocumentAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(T document, TimeSpan? duration)
        {
            throw new NotImplementedException();
        }

        Task<LockAcquisitionResult> IDistributedLockManager.LockDocumentAsyncNew<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(T document, TimeSpan? duration)
        {
            throw new NotImplementedException();
        }

        Task<IMongoLockScope> IDistributedLockManager.LockScopedAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(T document, TimeSpan? duration)
        {
            throw new NotImplementedException();
        }

        Task IDistributedLockManager.ReleaseLockAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string recordKey, string holderId)
        {
            throw new NotImplementedException();
        }

        Task IDistributedLockManager.ReleaseLockAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(T document, string holderId)
        {
            throw new NotImplementedException();
        }

        Task<bool> IDistributedLockManager.RenewLockAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string recordKey, string holderId, TimeSpan? extendBy)
        {
            throw new NotImplementedException();
        }

        private static string GenerateHolderId(string key) =>
            $"{Environment.MachineName}-{Environment.ProcessId}-{key}-{Guid.NewGuid():N}";
    }
}
