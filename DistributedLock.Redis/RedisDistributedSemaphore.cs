using Medallion.Threading.Internal;
using Medallion.Threading.Redis.Primitives;
using Medallion.Threading.Redis.RedLock;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Redis;

/// <summary>
/// Implements a <see cref="IDistributedSemaphore"/> using Redis.
/// </summary>
public sealed partial class RedisDistributedSemaphore : IInternalDistributedSemaphore<RedisDistributedSemaphoreHandle>
{
    /// <summary>
    /// Note: while we store this as a list to simplify the interactions with the RedLock components, in fact the semaphore
    /// algorithm only works with a single database. With multiple databases, we risk violating our <see cref="MaxCount"/>.
    /// For example, with 3 dbs and 2 tickets, we can have 3 users acquiring AB, BC, and AC. Each database sees 2 tickets taken!
    /// </summary>
    private readonly IReadOnlyList<IDatabase> _databases;
    private readonly RedisDistributedLockOptions _options;

    /// <summary>
    /// Constructs a semaphore named <paramref name="key"/> using the provided <paramref name="maxCount"/>, <paramref name="database"/>, and <paramref name="options"/>.
    /// </summary>
    public RedisDistributedSemaphore(RedisKey key, int maxCount, IDatabase database, Action<RedisDistributedSynchronizationOptionsBuilder>? options = null)
    {
        if (key == default(RedisKey)) { throw new ArgumentNullException(nameof(key)); }
        if (maxCount < 1) { throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "must be positive"); }
        this._databases = new[] { database ?? throw new ArgumentNullException(nameof(database)) };

        this.Key = key;
        this.MaxCount = maxCount;
        this._options = RedisDistributedSynchronizationOptionsBuilder.GetOptions(options);
    }

    internal RedisKey Key { get; }

    /// <summary>
    /// Implements <see cref="IDistributedSemaphore.Name"/>
    /// </summary>
    public string Name => this.Key.ToString();

    /// <summary>
    /// Implements <see cref="IDistributedSemaphore.MaxCount"/>
    /// </summary>
    public int MaxCount { get; }

    ValueTask<RedisDistributedSemaphoreHandle?> IInternalDistributedSemaphore<RedisDistributedSemaphoreHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken) =>
        BusyWaitHelper.WaitAsync(
            state: this,
            tryGetValue: (@this, cancellationToken) => @this.TryAcquireAsync(cancellationToken),
            timeout: timeout,
            minSleepTime: this._options.MinBusyWaitSleepTime,
            maxSleepTime: this._options.MaxBusyWaitSleepTime,
            cancellationToken: cancellationToken
        );

    private async ValueTask<RedisDistributedSemaphoreHandle?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        var primitive = new RedisSemaphorePrimitive(this.Key, this.MaxCount, this._options.RedLockTimeouts);
        var tryAcquireTasks = await new RedLockAcquire(primitive, this._databases, cancellationToken).TryAcquireAsync().ConfigureAwait(false);
        return tryAcquireTasks != null
            ? new RedisDistributedSemaphoreHandle(new RedLockHandle(primitive, tryAcquireTasks, extensionCadence: this._options.ExtensionCadence, expiry: this._options.RedLockTimeouts.Expiry))
            : null;
    }
}
