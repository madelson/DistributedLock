using Medallion.Threading.Internal;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Medallion.Threading.MongoDB;

/// <summary>
/// Implements a <see cref="IDistributedLock" /> using MongoDB.
/// </summary>
public sealed partial class MongoDistributedLock : IInternalDistributedLock<MongoDistributedLockHandle>
{
    internal const string DefaultCollectionName = "distributed.locks";

    private static readonly DateTime EpochUtc = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// ActivitySource for distributed tracing and diagnostics
    /// </summary>
    internal static readonly ActivitySource ActivitySource = new("DistributedLock.MongoDB", "1.0.0");

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
        // From what I can tell, modern (and all supported) MongoDB versions have no limits on index keys or
        // _id lengths other than the 16MB document limit. This is so high that providing "safe name" functionality as a fallback doesn't
        // see worth it.
        this.Key = key ?? throw new ArgumentNullException(nameof(key));
        this._options = MongoDistributedSynchronizationOptionsBuilder.GetOptions(options);
    }

    ValueTask<MongoDistributedLockHandle?> IInternalDistributedLock<MongoDistributedLockHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken) =>
        BusyWaitHelper.WaitAsync(this,
            (@this, ct) => @this.TryAcquireAsync(ct),
            timeout,
            minSleepTime: this._options.MinBusyWaitSleepTime,
            maxSleepTime: this._options.MaxBusyWaitSleepTime,
            cancellationToken);

    private async ValueTask<MongoDistributedLockHandle?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity(nameof(MongoDistributedLock) + ".TryAcquire");
        activity?.SetTag("lock.key", this.Key);
        activity?.SetTag("lock.collection", this._collectionName);

        var collection = this._database.GetCollection<MongoLockDocument>(this._collectionName);

        // Ensure indexes exist (TTL cleanup); do this at most once per process per (db, collection)
        await EnsureIndexesCreatedAsync(collection).ConfigureAwait(false);

        // Use a unique token per acquisition attempt (like Redis' value token)
        var lockId = Guid.NewGuid().ToString("N");
        
        // We avoid exception-driven contention (DuplicateKey) by using a single upsert on {_id == Key}
        // and an update pipeline that only overwrites fields when the existing lock is expired.
        // This is conceptually similar to Redis: SET key value NX PX <expiry>.
        var filter = Builders<MongoLockDocument>.Filter.Eq(d => d.Id, this.Key);
        var update = CreateAcquireUpdate(lockId, this._options.Expiry);
        var options = new FindOneAndUpdateOptions<MongoLockDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var result = SyncViaAsync.IsSynchronous
            ? collection.FindOneAndUpdate(filter, update, options, cancellationToken)
            : await collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken).ConfigureAwait(false);

        // Verify we actually got the lock
        if (result?.LockId == lockId)
        {
            activity?.SetTag("lock.acquired", true);
            activity?.SetTag("lock.fencing_token", result.FencingToken);
            return new(new(this, lockId, collection), result.FencingToken);
        }
        activity?.SetTag("lock.acquired", false);
        return null;
    }

    private static UpdateDefinition<MongoLockDocument> CreateAcquireUpdate(string lockId, TimeoutValue expiry)
    {
        Invariant.Require(!expiry.IsInfinite);

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
                { "amount", expiry.InMilliseconds }
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

        var success = await lazy.Value.AwaitSyncOverAsync().ConfigureAwait(false);
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
    /// Inner handle that performs actual lock management and release.
    /// Separated from the outer handle so it can be registered with ManagedFinalizerQueue.
    /// </summary>
    internal sealed class InnerHandle : IAsyncDisposable, LeaseMonitor.ILeaseHandle
    {
        private readonly MongoDistributedLock _lock;
        private readonly string _lockId;
        private readonly IMongoCollection<MongoLockDocument> _collection;
        private readonly LeaseMonitor _monitor;
        
        public CancellationToken HandleLostToken => this._monitor.HandleLostToken;

        TimeoutValue LeaseMonitor.ILeaseHandle.LeaseDuration => this._lock._options.Expiry;

        // todo what if inf?
        TimeoutValue LeaseMonitor.ILeaseHandle.MonitoringCadence => this._lock._options.ExtensionCadence;

        public InnerHandle(MongoDistributedLock @lock, string lockId, IMongoCollection<MongoLockDocument> collection)
        {
            this._lock = @lock;
            this._lockId = lockId;
            // important to set this last, since the monitor constructor will read other fields of this
            this._monitor = new(this);
            this._collection = collection;
        }

        public async ValueTask DisposeAsync()
        {
            try { await this._monitor.DisposeAsync().ConfigureAwait(false); }
            finally { await this.ReleaseLockAsync().ConfigureAwait(false); }
        }

        private async ValueTask ReleaseLockAsync()
        {
            var filter = Builders<MongoLockDocument>.Filter.Eq(d => d.Id, this._lock.Key) & Builders<MongoLockDocument>.Filter.Eq(d => d.LockId, this._lockId);
            if (SyncViaAsync.IsSynchronous)
            {
                this._collection.DeleteOne(filter);
            }
            else
            {
                await this._collection.DeleteOneAsync(filter).ConfigureAwait(false);
            }
        }

        async Task<LeaseMonitor.LeaseState> LeaseMonitor.ILeaseHandle.RenewOrValidateLeaseAsync(CancellationToken cancellationToken)
        {
            var filter = Builders<MongoLockDocument>.Filter.Eq(d => d.Id, this._lock.Key) & Builders<MongoLockDocument>.Filter.Eq(d => d.LockId, this._lockId);

            // Use server time ($$NOW) for expiry to avoid client clock skew.
            var newExpiresAt = new BsonDocument(
                "$dateAdd",
                new BsonDocument
                {
                    { "startDate", "$$NOW" },
                    { "unit", "millisecond" },
                    { "amount", this._lock._options.Expiry.InMilliseconds }
                }
            );
            var update = new PipelineUpdateDefinition<MongoLockDocument>(
                new[] { new BsonDocument("$set", new BsonDocument("expiresAt", newExpiresAt)) }
            );

            var result = await this._collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
            return result.MatchedCount > 0 ? LeaseMonitor.LeaseState.Renewed : LeaseMonitor.LeaseState.Lost;
        }
    }
}