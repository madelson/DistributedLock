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
    /// and uses the provided <paramref name="options" />. Locks will be stored in a collection named "distributed.locks" by default.
    /// </summary>
    public MongoDistributedSynchronizationProvider(IMongoDatabase database, Action<MongoDistributedSynchronizationOptionsBuilder>? options = null)
        : this(database, MongoDistributedLock.DefaultCollectionName, options) { }

    /// <summary>
    /// Constructs a <see cref="MongoDistributedSynchronizationProvider" /> that connects to the provided <paramref name="database" />,
    /// stores locks in the specified <paramref name="collectionName" />, and uses the provided <paramref name="options" />.
    /// </summary>
    public MongoDistributedSynchronizationProvider(IMongoDatabase database, string collectionName, Action<MongoDistributedSynchronizationOptionsBuilder>? options = null)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _collectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
        _options = options;
    }

    /// <summary>
    /// Creates a <see cref="MongoDistributedLock" /> using the given <paramref name="name" />.
    /// </summary>
    public MongoDistributedLock CreateLock(string name)
    {
        return new(name, _database, _collectionName, _options);
    }

    IDistributedLock IDistributedLockProvider.CreateLock(string name)
    {
        return this.CreateLock(name);
    }
}