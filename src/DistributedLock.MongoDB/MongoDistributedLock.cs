using Medallion.Threading.Internal;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;

#if NET8_0_OR_GREATER
using System.Diagnostics;
#endif

namespace Medallion.Threading.MongoDB;

/// <summary>
/// Implements a <see cref="IDistributedLock" /> using MongoDB.
/// </summary>
public sealed partial class MongoDistributedLock : IInternalDistributedLock<MongoDistributedLockHandle>
{
#if !NETSTANDARD2_1_OR_GREATER && !NET8_0_OR_GREATER
    private static readonly DateTime EpochUtc = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
#endif

#if NET8_0_OR_GREATER
    /// <summary>
    /// ActivitySource for distributed tracing and diagnostics
    /// </summary>
    private static readonly ActivitySource ActivitySource = new("DistributedLock.MongoDB", "1.0.0");
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
        if (this._options.UseAdaptiveBackoff)
        {
            return this.AdaptiveBusyWaitAsync(timeout, cancellationToken);
        }

        return BusyWaitHelper.WaitAsync(this,
            (@this, ct) => @this.TryAcquireAsync(ct),
            timeout,
            this._options.MinBusyWaitSleepTime,
            this._options.MaxBusyWaitSleepTime,
            cancellationToken);
    }

    private async ValueTask<MongoDistributedLockHandle?> AdaptiveBusyWaitAsync(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        // Try immediately first
        var result = await this.TryAcquireAsync(cancellationToken).ConfigureAwait(false);
        if (result != null || timeout.IsZero)
        {
            return result;
        }

        using var timeoutCts = timeout.IsInfinite ? null : new CancellationTokenSource(timeout.TimeSpan);
        using var linkedCts = timeoutCts != null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
            : null;
        var effectiveToken = linkedCts?.Token ?? cancellationToken;

        var minMs = this._options.MinBusyWaitSleepTime.InMilliseconds;
        var maxMs = this._options.MaxBusyWaitSleepTime.InMilliseconds;
        var consecutiveFailures = 0;
        const double BackoffMultiplier = 1.5;
#if NET8_0_OR_GREATER
        var random = Random.Shared;
#else
        var random = new Random(Guid.NewGuid().GetHashCode());
#endif

        while (!effectiveToken.IsCancellationRequested)
        {
            // Exponential backoff with jitter
            var backoffMs = minMs * Math.Pow(BackoffMultiplier, Math.Min(consecutiveFailures, 10));
            var sleepMs = Math.Min(backoffMs, maxMs);
            // Add jitter (±20%) to prevent thundering herd
            var jitter = (random.NextDouble() - 0.5) * 0.4 * sleepMs;
            var finalSleepMs = Math.Max(minMs, sleepMs + jitter);

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(finalSleepMs), effectiveToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested)
            {
                // Timeout expired, try one last time
                return await this.TryAcquireAsync(cancellationToken).ConfigureAwait(false);
            }

            try
            {
                result = await this.TryAcquireAsync(effectiveToken).ConfigureAwait(false);
                if (result != null)
                {
                    return result;
                }
                consecutiveFailures++;
            }
            catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested)
            {
                return null;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return null;
    }

    private async ValueTask<MongoDistributedLockHandle?> TryAcquireAsync(CancellationToken cancellationToken)
    {
#if NET8_0_OR_GREATER
        using var activity = ActivitySource.StartActivity("MongoDistributedLock.TryAcquire");
        activity?.SetTag("lock.key", this.Key);
        activity?.SetTag("lock.collection", this._collectionName);
#endif

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
#if NET8_0_OR_GREATER
            activity?.SetTag("lock.acquired", true);
            activity?.SetTag("lock.fencing_token", result.FencingToken);
#endif
            return new(collection,
                this.Key,
                lockId,
                result.FencingToken,
                this._options.Expiry,
                this._options.ExtensionCadence);
        }

#if NET8_0_OR_GREATER
        activity?.SetTag("lock.acquired", false);
#endif
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
                { "lockId", new BsonDocument("$cond", new BsonArray { expiredOrMissing, lockId, "$lockId" }) },
                { "expiresAt", new BsonDocument("$cond", new BsonArray { expiredOrMissing, newExpiresAt, "$expiresAt" }) },
                { "acquiredAt", new BsonDocument("$cond", new BsonArray { expiredOrMissing, "$$NOW", "$acquiredAt" }) },
                { "fencingToken", new BsonDocument("$cond", new BsonArray { expiredOrMissing, newFencingToken, "$fencingToken" }) }
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
                Name = "expiresAt_ttl"
            };
            var indexModel = new CreateIndexModel<MongoLockDocument>(indexKeys, indexOptions);
            await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }
        catch (MongoCommandException ex) when (
            ex.CodeName is "IndexOptionsConflict" or "IndexKeySpecsConflict" or "IndexAlreadyExists")
        {
            // Index already exists with same or different options - this is acceptable.
            // The existing index will still handle TTL cleanup.
        }
        catch (MongoException)
        {
            // Other MongoDB errors (network, auth, etc.) - swallow to avoid blocking lock acquisition.
            // The lock will still work correctly; TTL cleanup is a best-effort optimization.
        }
    }
}