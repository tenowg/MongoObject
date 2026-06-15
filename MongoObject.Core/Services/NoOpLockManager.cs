using MongoObject.Core.Data;
using MongoObject.Core.Exceptions;
using MongoObject.Core.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace MongoObject.Core.Services
{
    internal class NoOpLockManager() : IDistributedLockManager
    {
        public async Task<LockAcquisitionResult> LockDocumentAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(T document, TimeSpan? duration = null)
            where T : class, IDocumentFile, new()
        {
            return LockAcquisitionResult.NoLock();
        }

        public async Task<LockAcquisitionResult> LockDocumentAsyncNew<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(T document, TimeSpan? duration = null)
            where T : class, IDocumentFile, new()
        {
            return LockAcquisitionResult.NoLock();
        }

        public async Task<bool> IsLockedByOther<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(IMongoLockScope? scope, T document) where T : class, IDocumentFile, new()
        {
            return false;
        }

        public async Task<LockMetadata?> GetLock(string key)
        {
            return null;
        }

        public async Task<LockMetadata?> GetLock(IMongoLockScope recordKey)
        {
            return null;
        }

        public async Task ReleaseLockAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(T document, string holderId) where T : class, IDocumentFile, new()
        {
        }

        public async Task ReleaseLockAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string recordKey, string holderId) where T : class, IDocumentFile, new()
        {
        }

        /// <summary>
        /// Acquires a lock and returns a scope that automatically releases it when disposed.
        /// Recommended for most use cases (cleanest syntax).
        /// </summary>
        public async Task<IMongoLockScope> LockScopedAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(
            T document,
            TimeSpan? duration = null)
            where T : class, IDocumentFile, new()
        {
            throw new NoLockManagerInstalled();
        }

        public async Task<bool> RenewLockAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string recordKey, string holderId, TimeSpan? extendBy = null) where T : class, IDocumentFile, new()
        {
            throw new NoLockManagerInstalled();
        }

        private static string GenerateHolderId(string key) =>
            $"{Environment.MachineName}-{Environment.ProcessId}-{key}-{Guid.NewGuid():N}";
    }
}
