using Medallion.Threading.Internal;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;

namespace Medallion.Threading.MongoDB;

/// <summary>
/// Implements a <see cref="IDistributedLock" /> using MongoDB.
/// </summary>
public sealed partial class MongoDistributedLock : IInternalDistributedLock<MongoDistributedLockHandle>
{
#if !NETSTANDARD2_1_OR_GREATER && !NET8_0_OR_GREATER
    private static readonly DateTime EpochUtc = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
#endif

    // We want to ensure indexes at most once per process per (db, collection)
    private static readonly ConcurrentDictionary<string, Lazy<Task>> IndexInitializationTasks = new(StringComparer.Ordinal);

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
    /// The locks will be stored in a collection named "DistributedLocks" by default.
    /// </summary>
    public MongoDistributedLock(string key, IMongoDatabase database, Action<MongoDistributedSynchronizationOptionsBuilder>? options = null)
        : this(key, database, "DistributedLocks", options) { }

    /// <summary>
    /// Constructs a lock named <paramref name="key" /> using the provided <paramref name="database" />, <paramref name="collectionName" />, and
    /// <paramref name="options" />.
    /// </summary>
    public MongoDistributedLock(string key, IMongoDatabase database, string collectionName, Action<MongoDistributedSynchronizationOptionsBuilder>? options = null)
    {
        if (string.IsNullOrEmpty(key)) { throw new ArgumentNullException(nameof(key)); }
        this._database = database ?? throw new ArgumentNullException(nameof(database));
        this._collectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
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

        var result = await collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken).ConfigureAwait(false);

        // Verify we actually got the lock
        if (result != null && result.LockId == lockId)
        {
            return new(collection,
                this.Key,
                lockId,
                this._options.Expiry,
                this._options.ExtensionCadence);
        }

        return null;
    }

    private static UpdateDefinition<MongoLockDocument> CreateAcquireUpdate(string lockId, int expiryMs)
    {
        // expired := ifNull(expiresAt, epoch) <= $$NOW
        var expiredOrMissing = new BsonDocument(
            "$lte",
            new BsonArray
            {
                new BsonDocument("$ifNull", new BsonArray { "$expiresAt", new BsonDateTime(
#if NETSTANDARD2_1_OR_GREATER || NET8_0_OR_GREATER
                    DateTime.UnixEpoch
#else
                    EpochUtc
#endif
                ) }),
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

        var setStage = new BsonDocument(
            "$set",
            new BsonDocument
            {
                // Only overwrite lock fields when the previous lock is expired/missing
                { "lockId", new BsonDocument("$cond", new BsonArray { expiredOrMissing, lockId, "$lockId" }) },
                { "expiresAt", new BsonDocument("$cond", new BsonArray { expiredOrMissing, newExpiresAt, "$expiresAt" }) },
                { "acquiredAt", new BsonDocument("$cond", new BsonArray { expiredOrMissing, "$$NOW", "$acquiredAt" }) }
            }
        );

        return new PipelineUpdateDefinition<MongoLockDocument>(new[] { setStage });
    }

    private static Task EnsureIndexesCreatedAsync(IMongoCollection<MongoLockDocument> collection)
    {
        // Best-effort TTL index to clean up expired rows over time.
        // Note: TTL monitors run on a schedule; correctness MUST NOT depend on this.
        var databaseName = collection.Database.DatabaseNamespace.DatabaseName;
        var key = databaseName + "/" + collection.CollectionNamespace.CollectionName;

        var lazy = IndexInitializationTasks.GetOrAdd(key, _ => new Lazy<Task>(() => CreateIndexesAsync(collection)));
        return lazy.Value;
    }

    private static async Task CreateIndexesAsync(IMongoCollection<MongoLockDocument> collection)
    {
        try
        {
            var indexKeys = Builders<MongoLockDocument>.IndexKeys.Ascending(d => d.ExpiresAt);
            var indexOptions = new CreateIndexOptions
            {
                // TTL cleanup: remove documents once expiresAt < now
                ExpireAfter = TimeSpan.Zero,
            };
            var indexModel = new CreateIndexModel<MongoLockDocument>(indexKeys, indexOptions);
            await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Index may already exist, or server may reject options (e.g., conflicts). Ignore.
        }
    }
}