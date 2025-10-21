using Medallion.Threading.Internal;
using MongoDB.Driver;

namespace Medallion.Threading.MongoDB;

/// <summary>
/// Implements a <see cref="IDistributedLock" /> using MongoDB.
/// </summary>
public sealed partial class MongoDistributedLock : IInternalDistributedLock<MongoDistributedLockHandle>
{
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
    public string Name => Key;

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
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _collectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
        Key = key;
        _options = MongoDistributedSynchronizationOptionsBuilder.GetOptions(options);
    }

    ValueTask<MongoDistributedLockHandle?> IInternalDistributedLock<MongoDistributedLockHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        return BusyWaitHelper.WaitAsync(this,
            (@this, ct) => @this.TryAcquireAsync(ct),
            timeout,
            _options.MinBusyWaitSleepTime,
            _options.MaxBusyWaitSleepTime,
            cancellationToken);
    }

    private async ValueTask<MongoDistributedLockHandle?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<MongoLockDocument>(_collectionName);

        // Ensure index exists for efficient queries
        await EnsureIndexAsync(collection, cancellationToken).ConfigureAwait(false);
        var lockId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(_options.Expiry.TimeSpan);

        // Filter: lock with this key doesn't exist OR has expired
        var filter = Builders<MongoLockDocument>.Filter.Or(Builders<MongoLockDocument>.Filter.Eq(d => d.Id, Key) & Builders<MongoLockDocument>.Filter.Lt(d => d.ExpiresAt, now),
            Builders<MongoLockDocument>.Filter.Eq(d => d.Id, Key) & Builders<MongoLockDocument>.Filter.Exists(d => d.ExpiresAt, false));
        var update = Builders<MongoLockDocument>.Update
                                                .Set(d => d.Id, Key)
                                                .Set(d => d.LockId, lockId)
                                                .Set(d => d.ExpiresAt, expiresAt)
                                                .Set(d => d.AcquiredAt, now);
        var options = new FindOneAndUpdateOptions<MongoLockDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After };
        try
        {
            var result = await collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken).ConfigureAwait(false);

            // Verify we actually got the lock
            if (result != null && result.LockId == lockId)
            {
                return new(_database,
                    _collectionName,
                    Key,
                    lockId,
                    _options.Expiry,
                    _options.ExtensionCadence);
            }
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Lock is already held by someone else
            return null;
        }
        catch (MongoCommandException)
        {
            // Lock is already held
            return null;
        }
        return null;
    }

    private static async Task EnsureIndexAsync(IMongoCollection<MongoLockDocument> collection, CancellationToken cancellationToken)
    {
        try
        {
            var indexKeys = Builders<MongoLockDocument>.IndexKeys.Ascending(d => d.ExpiresAt);
            var indexOptions = new CreateIndexOptions { Background = true };
            var indexModel = new CreateIndexModel<MongoLockDocument>(indexKeys, indexOptions);
            await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Index might already exist, ignore errors
        }
    }
}