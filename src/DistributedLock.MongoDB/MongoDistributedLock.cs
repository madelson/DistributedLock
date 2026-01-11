using Medallion.Threading.Internal;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Medallion.Threading.MongoDB;

/// <summary>
/// Implements a <see cref="IDistributedLock" /> using MongoDB.
/// </summary>
public sealed partial class MongoDistributedLock : IInternalDistributedLock<MongoDistributedLockHandle>
{
    internal const string DefaultCollectionName = "distributed.locks";
    /// <summary>
    /// MongoDB _id field maximum length in bytes.
    /// See https://www.mongodb.com/docs/manual/reference/limits/#mongodb-limit-Index-Key-Limit
    /// </summary>
    private const int MaxKeyLength = 255;

    private static readonly DateTime EpochUtc = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// ActivitySource for distributed tracing and diagnostics
    /// </summary>
    private static readonly ActivitySource ActivitySource = new("DistributedLock.MongoDB", "1.0.0");

    // We want to ensure indexes are created at most once per process per (database, collection)
    private static readonly ConcurrentDictionary<string, Lazy<Task<bool>>> IndexInitializationTasks = new(StringComparer.Ordinal);

    private readonly string _collectionName;
    private readonly IMongoDatabase _database;
    private readonly MongoDistributedLockOptions _options;

    /// <summary>
    /// The MongoDB key used to implement the lock
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Implements <see cref="IDistributedLock.Name" />
    /// </summary>
    public string Name => this.Key;

    /// <summary>
    /// Constructs a lock named <paramref name="key" /> using the provided <paramref name="database" /> and <paramref name="options" />.
    /// The locks will be stored in a collection named "distributed.locks" by default.
    /// </summary>
    public MongoDistributedLock(string key, IMongoDatabase database, Action<MongoDistributedSynchronizationOptionsBuilder>? options = null)
        : this(key, database, DefaultCollectionName, options) { }

    /// <summary>
    /// Constructs a lock named <paramref name="key" /> using the provided <paramref name="database" />, <paramref name="collectionName" />, and
    /// <paramref name="options" />.
    /// </summary>
    public MongoDistributedLock(string key, IMongoDatabase database, string collectionName, Action<MongoDistributedSynchronizationOptionsBuilder>? options = null)
    {
        this._database = database ?? throw new ArgumentNullException(nameof(database));
        this._collectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));

        if (key == null) { throw new ArgumentNullException(nameof(key)); }
        ValidateKey(key);
        this.Key = key;
        this._options = MongoDistributedSynchronizationOptionsBuilder.GetOptions(options);
    }

    ValueTask<MongoDistributedLockHandle?> IInternalDistributedLock<MongoDistributedLockHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        return BusyWaitHelper.WaitAsync(this,
            (@this, ct) => @this.TryAcquireAsync(ct),
            timeout,
            this._options.MinBusyWaitSleepTime,
            this._options.MaxBusyWaitSleepTime,
            cancellationToken);
    }

    private async ValueTask<MongoDistributedLockHandle?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("MongoDistributedLock.TryAcquire");
        activity?.SetTag("lock.key", this.Key);
        activity?.SetTag("lock.collection", this._collectionName);

        var collection = this._database.GetCollection<MongoLockDocument>(this._collectionName);

        // Ensure indexes exist (TTL cleanup); do this at most once per process per (db, collection)
        await EnsureIndexesCreatedAsync(collection).ConfigureAwait(false);

        // Use a unique token per acquisition attempt (like Redis' value token)
        var lockId = Guid.NewGuid().ToString("N");
        var expiryMs = this._options.Expiry.InMilliseconds;

        // We avoid exception-driven contention (DuplicateKey) by using a single upsert on {_id == Key}
        // and an update pipeline that only overwrites fields when the existing lock is expired.
        // This is conceptually similar to Redis: SET key value NX PX <expiry>.
        var filter = Builders<MongoLockDocument>.Filter.Eq(d => d.Id, this.Key);
        var update = CreateAcquireUpdate(lockId, expiryMs);
        var options = new FindOneAndUpdateOptions<MongoLockDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        MongoLockDocument? result;
        if (SyncViaAsync.IsSynchronous)
        {
            result = collection.FindOneAndUpdate(filter, update, options, cancellationToken);
        }
        else
        {
            result = await collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken).ConfigureAwait(false);
        }

        // Verify we actually got the lock
        if (result is not null && result.LockId == lockId)
        {
            activity?.SetTag("lock.acquired", true);
            activity?.SetTag("lock.fencing_token", result.FencingToken);
            return new(collection,
                this.Key,
                lockId,
                result.FencingToken,
                this._options.Expiry,
                this._options.ExtensionCadence);
        }
        activity?.SetTag("lock.acquired", false);
        return null;
    }

    private static UpdateDefinition<MongoLockDocument> CreateAcquireUpdate(string lockId, int expiryMs)
    {
        // expired := ifNull(expiresAt, epoch) <= $$NOW
        var expiredOrMissing = new BsonDocument(
            "$lte",
            new BsonArray
            {
                new BsonDocument("$ifNull", new BsonArray { "$expiresAt", new BsonDateTime(EpochUtc) }),
                "$$NOW"
            }
        );

        var newExpiresAt = new BsonDocument(
            "$dateAdd",
            new BsonDocument
            {
                { "startDate", "$$NOW" },
                { "unit", "millisecond" },
                { "amount", expiryMs }
            }
        );

        // Increment fencing token only when acquiring a new lock
        var newFencingToken = new BsonDocument(
            "$add",
            new BsonArray
            {
                new BsonDocument("$ifNull", new BsonArray { "$fencingToken", 0L }),
                1L
            }
        );

        var setStage = new BsonDocument(
            "$set",
            new BsonDocument
            {
                // Only overwrite lock fields when the previous lock is expired/missing
                { nameof(lockId), new BsonDocument("$cond", new BsonArray { expiredOrMissing, lockId, "$lockId" }) },
                { "expiresAt", new BsonDocument("$cond", new BsonArray { expiredOrMissing, newExpiresAt, "$expiresAt" }) },
                { "acquiredAt", new BsonDocument("$cond", new BsonArray { expiredOrMissing, "$$NOW", "$acquiredAt" }) },
                { "fencingToken", new BsonDocument("$cond", new BsonArray { expiredOrMissing, newFencingToken, "$fencingToken" }) }
            }
        );

        return new PipelineUpdateDefinition<MongoLockDocument>(new[] { setStage });
    }

    private static async Task EnsureIndexesCreatedAsync(IMongoCollection<MongoLockDocument> collection)
    {
        // Best-effort TTL index to clean up expired rows over time.
        // Note: TTL monitors run on a schedule; correctness MUST NOT depend on this.
        var databaseName = collection.Database.DatabaseNamespace.DatabaseName;
        // include the hash code of the settings to differentiate between different clusters/clients
        // that happen to use the same database/collection names.
        // While GetHashCode() isn't perfect, it should be sufficient to distinguish between different clients/settings
        // in valid use-cases (e.g. diff connection strings).
        var clientSettingsHash = collection.Database.Client.Settings.GetHashCode();
        var key = clientSettingsHash + "|" + databaseName + "/" + collection.CollectionNamespace.CollectionName;

        var lazy = IndexInitializationTasks.GetOrAdd(key, _ => new(() => CreateIndexesAsync(collection)));

        var task = lazy.Value;
        var success = await task.AwaitSyncOverAsync().ConfigureAwait(false);
        if (!success)
        {
            // If the task failed (returned false), we remove it so we can try again next time.
            // Note: worst case we remove a *new* valid task if a race happens, which is fine (just extra work).
            IndexInitializationTasks.As<ICollection<KeyValuePair<string, Lazy<Task<bool>>>>>().Remove(new KeyValuePair<string, Lazy<Task<bool>>>(key, lazy));
        }
    }

    private static async Task<bool> CreateIndexesAsync(IMongoCollection<MongoLockDocument> collection)
    {
        try
        {
            var indexKeys = Builders<MongoLockDocument>.IndexKeys.Ascending(d => d.ExpiresAt);
            var indexOptions = new CreateIndexOptions
            {
                // TTL cleanup: remove documents once expiresAt < now
                ExpireAfter = TimeSpan.Zero,
                Name = "expiresAt_ttl"
            };
            var indexModel = new CreateIndexModel<MongoLockDocument>(indexKeys, indexOptions);
            await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: CancellationToken.None).ConfigureAwait(false);
            return true;
        }
        catch (MongoCommandException ex) when (ex.CodeName is "IndexOptionsConflict" or "IndexKeySpecsConflict" or "IndexAlreadyExists")
        {
            // Index already exists with same or different options - this is acceptable.
            // The existing index will still handle TTL cleanup.
            return true;
        }
        catch (MongoException)
        {
            // Other MongoDB errors (network, auth, etc.) - swallow to avoid blocking lock acquisition.
            // The lock will still work correctly; TTL cleanup is a best-effort optimization.
            return false;
        }
    }

    /// <summary>
    /// Validates that a key is valid for use as an exact MongoDB key.
    /// </summary>
    private static void ValidateKey(string key)
    {
        if (key == null) { throw new ArgumentNullException(nameof(key)); }
        if (key.Length == 0) { throw new FormatException($"{nameof(key)}: must not be empty"); }

        var byteCount = Encoding.UTF8.GetByteCount(key);
        if (byteCount > MaxKeyLength)
        {
            throw new FormatException($"{nameof(key)}: must be at most {MaxKeyLength} bytes when encoded as UTF-8 (was {byteCount} bytes)");
        }
    }
}