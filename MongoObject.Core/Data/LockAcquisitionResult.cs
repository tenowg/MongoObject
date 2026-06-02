
namespace MongoObject.Core.Data
{
    public record LockAcquisitionResult(bool SuccessResult, string? HolderId, DateTime? ExpiresAt = null, string? ErrorMessage = null)
    {
        public static LockAcquisitionResult Failed(string message, string? holderId = null, DateTime? expiresAt = null) => new(false, holderId, expiresAt, ErrorMessage: message);
        public static LockAcquisitionResult Success(string? holderId, DateTime? expiresAt) => new(true, holderId, expiresAt);
    }
}
