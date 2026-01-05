using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Medallion.Threading.MongoDB;

/// <summary>
/// Represents a lock document stored in MongoDB
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class MongoLockDocument
{
    /// <summary>
    /// When the lock was acquired (UTC)
    /// </summary>
    [BsonElement("acquiredAt")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTime AcquiredAt { get; set; }

    /// <summary>
    /// When the lock expires (UTC)
    /// </summary>
    [BsonElement("expiresAt")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Monotonically increasing fencing token for safe resource access
    /// </summary>
    [BsonElement("fencingToken")]
    public long FencingToken { get; set; }

    /// <summary>
    /// The lock name/key (MongoDB document ID)
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier for this lock acquisition
    /// </summary>
    [BsonElement("lockId")]
    [BsonRepresentation(BsonType.String)]
    public string LockId { get; set; } = string.Empty;
}