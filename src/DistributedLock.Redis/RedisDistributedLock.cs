using Medallion.Threading.Internal;
using Medallion.Threading.Redis.Primitives;
using Medallion.Threading.Redis.RedLock;
using StackExchange.Redis;

namespace Medallion.Threading.Redis;

/// <summary>
/// Implements a <see cref="IDistributedLock"/> using Redis. Can leverage multiple servers via the RedLock algorithm.
/// </summary>
public sealed partial class RedisDistributedLock : IInternalDistributedLock<RedisDistributedLockHandle>
{
    private readonly IReadOnlyList<IDatabase> _databases;
    private readonly RedisDistributedLockOptions _options;
    
    /// <summary>
    /// Constructs a lock named <paramref name="key"/> using the provided <paramref name="database"/> and <paramref name="options"/>.
    /// </summary>
    public RedisDistributedLock(RedisKey key, IDatabase database, Action<RedisDistributedSynchronizationOptionsBuilder>? options = null)
        : this(key, new[] { database ?? throw new ArgumentNullException(nameof(database)) }, options)
    {
    }

    /// <summary>
    /// Constructs a lock named <paramref name="key"/> using the provided <paramref name="databases"/> and <paramref name="options"/>.
    /// </summary>
    public RedisDistributedLock(RedisKey key, IEnumerable<IDatabase> databases, Action<RedisDistributedSynchronizationOptionsBuilder>? options = null)
    {
        if (key == default(RedisKey)) { throw new ArgumentNullException(nameof(key)); }
        this._databases = ValidateDatabases(databases);

        this.Key = key;
        this._options = RedisDistributedSynchronizationOptionsBuilder.GetOptions(options);
    }

    internal static IReadOnlyList<IDatabase> ValidateDatabases(IEnumerable<IDatabase> databases)
    {
        var databasesArray = databases?.ToArray() ?? throw new ArgumentNullException(nameof(databases));
        if (databasesArray.Length == 0) { throw new ArgumentException("may not be empty", nameof(databases)); }
        if (databasesArray.Contains(null!)) { throw new ArgumentNullException(nameof(databases), "may not contain null"); }
        return databasesArray;
    }

    /// <summary>
    /// The Redis key used to implement the lock
    /// </summary>
    public RedisKey Key { get; }

    /// <summary>
    /// Implements <see cref="IDistributedLock.Name"/>
    /// </summary>
    public string Name => this.Key.ToString();

    ValueTask<RedisDistributedLockHandle?> IInternalDistributedLock<RedisDistributedLockHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken) =>
        BusyWaitHelper.WaitAsync(
            state: this,
            tryGetValue: (@this, cancellationToken) => @this.TryAcquireAsync(cancellationToken),
            timeout: timeout,
            minSleepTime: this._options.MinBusyWaitSleepTime,
            maxSleepTime: this._options.MaxBusyWaitSleepTime,
            cancellationToken: cancellationToken
        );

    private async ValueTask<RedisDistributedLockHandle?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        var primitive = new RedisMutexPrimitive(this.Key, RedLockHelper.CreateLockId(), this._options.RedLockTimeouts);
        var tryAcquireTasks = await new RedLockAcquire(primitive, this._databases, cancellationToken).TryAcquireAsync().ConfigureAwait(false);
        return tryAcquireTasks != null 
            ? new RedisDistributedLockHandle(new RedLockHandle(primitive, tryAcquireTasks, extensionCadence: this._options.ExtensionCadence, expiry: this._options.RedLockTimeouts.Expiry)) 
            : null;
    }
}
