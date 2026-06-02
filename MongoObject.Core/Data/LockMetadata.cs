using MongoDB.Bson.Serialization.Attributes;

namespace MongoObject.Core.Data
{
    public class LockMetadata
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public string Id { get; set; } = string.Empty;
        public string? LockedBy { get; set; }
        public DateTime? LockExpiresAt { get; set; }
        public DateTime? LockAcquiredAt { get; set; }
    }
}
