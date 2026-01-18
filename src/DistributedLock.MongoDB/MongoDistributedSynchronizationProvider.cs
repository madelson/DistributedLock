using MongoDB.Driver;

namespace Medallion.Threading.MongoDB;

/// <summary>
/// Implements <see cref="IDistributedLockProvider" /> for <see cref="MongoDistributedLock" />.
/// </summary>
public sealed class MongoDistributedSynchronizationProvider : IDistributedLockProvider
{
    private readonly string _collectionName;
    private readonly IMongoDatabase _database;
    private readonly Action<MongoDistributedSynchronizationOptionsBuilder>? _options;

    /// <summary>
    /// Constructs a <see cref="MongoDistributedSynchronizationProvider" /> that connects to the provided <paramref name="database" />
    /// and uses the provided <paramref name="options" />. Locks will be stored in a collection named "distributed_locks" by default.
    /// </summary>
    public MongoDistributedSynchronizationProvider(IMongoDatabase database, Action<MongoDistributedSynchronizationOptionsBuilder>? options = null)
        : this(database, MongoDistributedLock.DefaultCollectionName, options) { }

    /// <summary>
    /// Constructs a <see cref="MongoDistributedSynchronizationProvider" /> that connects to the provided <paramref name="database" />,
    /// stores locks in the specified <paramref name="collectionName" />, and uses the provided <paramref name="options" />.
    /// </summary>
    public MongoDistributedSynchronizationProvider(IMongoDatabase database, string collectionName, Action<MongoDistributedSynchronizationOptionsBuilder>? options = null)
    {
        this._database = database ?? throw new ArgumentNullException(nameof(database));
        this._collectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
        this._options = options;
    }

    /// <summary>
    /// Creates a <see cref="MongoDistributedLock" /> using the given <paramref name="name" />.
    /// </summary>
    public MongoDistributedLock CreateLock(string name) => new(name, this._database, this._collectionName, this._options);

    IDistributedLock IDistributedLockProvider.CreateLock(string name) => this.CreateLock(name);
}