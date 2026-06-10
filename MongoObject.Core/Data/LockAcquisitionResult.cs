
namespace MongoObject.Core.Data
{
    public enum LockResult
    {
        Success,
        Failed,
        NoLockInstalled
    }

    public record LockAcquisitionResult(LockResult SuccessResult, string? HolderId = null, DateTime? ExpiresAt = null, string? ErrorMessage = null)
    {
        public static LockAcquisitionResult Failed(string message, string? holderId = null, DateTime? expiresAt = null) => new(LockResult.Failed, holderId, expiresAt, ErrorMessage: message);
        public static LockAcquisitionResult Success(string? holderId, DateTime? expiresAt) => new(LockResult.Success, holderId, expiresAt);
        public static LockAcquisitionResult NoLock() => new(LockResult.NoLockInstalled);
    }
}
