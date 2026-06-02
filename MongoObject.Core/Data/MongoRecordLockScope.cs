using Microsoft.Extensions.Logging;
using MongoObject.Core.Interfaces;
using MongoObject.Core.Services;
using System.Diagnostics.CodeAnalysis;

namespace MongoObject.Core.Data
{
    public class MongoRecordLockScope<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> : IMongoLockScope, IAsyncDisposable where T : class, IDocumentFile, new()
    {
        private readonly DistributedLockManager _manager;
        private readonly string _recordKey;
        private readonly string _holderId;
        private bool _disposed;
        ILogger<MongoRecordLockScope<T>>? _logger;

        public string HolderId { get { return _holderId; } }

        internal MongoRecordLockScope(
            DistributedLockManager manager,
            ILogger<MongoRecordLockScope<T>>? logger,
            string recordKey,
            string holderId)
        {
            _manager = manager;
            _recordKey = recordKey;
            _holderId = holderId;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                await _manager.ReleaseLockAsync<T>(_recordKey, _holderId);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Warning: Failed to release lock for {_recordKey}: {ex.Message}");
            }
            GC.SuppressFinalize(this);
        }
    }
}
