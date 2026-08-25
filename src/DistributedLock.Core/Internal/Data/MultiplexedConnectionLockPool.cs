namespace Medallion.Threading.Internal.Data;

/// <summary>
/// Implements a pool of <see cref="MultiplexedConnectionLock"/> instances
/// </summary>
#if DEBUG
public
#else
internal 
#endif
    sealed class MultiplexedConnectionLockPool<TConnectionSource>
    where TConnectionSource : notnull
{
    private readonly AsyncLock _lock = AsyncLock.Create();

    private readonly Dictionary<TConnectionSource, Queue<MultiplexedConnectionLock>> _poolsByConnectionSource;

    /// <summary>
    /// The number of times we've called <see cref="StoreOrDisposeLockAsync(TConnectionSource, MultiplexedConnectionLock, bool)"/>
    /// since we last called <see cref="PrunePoolsNoLockAsync"/>
    /// </summary>
    private uint _storeCountSinceLastPrune;
    /// <summary>
    /// The number of <see cref="MultiplexedConnectionLock"/>s stored in <see cref="_poolsByConnectionSource"/>
    /// </summary>
    private uint _pooledLockCount;

    public MultiplexedConnectionLockPool(Func<TConnectionSource, DatabaseConnection> connectionFactory, IEqualityComparer<TConnectionSource>? comparer = null)
    {
        this.ConnectionFactory = connectionFactory;
        this._poolsByConnectionSource = new(comparer);
    }

    internal Func<TConnectionSource, DatabaseConnection> ConnectionFactory { get; }

    public async ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync<TLockCookie>(
        TConnectionSource connectionSource,
        string name,
        TimeoutValue timeout,
        IDbSynchronizationStrategy<TLockCookie> strategy,
        TimeoutValue keepaliveCadence,
        CancellationToken cancellationToken)
        where TLockCookie : class
    {
        // opportunistic phase: see if we can use a connection that is already holding a lock
        // to acquire the current lock
        var existingLock = await this.GetExistingLockOrDefaultAsync(connectionSource).ConfigureAwait(false);
        if (existingLock != null)
        {
            var canSafelyDisposeExistingLock = false;
            try
            {
                var opportunisticResult = await TryAcquireAsync(existingLock, opportunistic: true).ConfigureAwait(false);
                if (opportunisticResult.Handle != null) { return opportunisticResult.Handle; }
                // this will always be false if handle is non-null, so we can set if afterwards
                canSafelyDisposeExistingLock = opportunisticResult.CanSafelyDispose;

                switch (opportunisticResult.Retry)
                {
                    case MultiplexedConnectionLockRetry.NoRetry:
                        return null;
                    case MultiplexedConnectionLockRetry.RetryOnThisLock:
                        var retryOnThisLockResult = await TryAcquireAsync(existingLock, opportunistic: false).ConfigureAwait(false);
                        canSafelyDisposeExistingLock = retryOnThisLockResult.CanSafelyDispose;
                        return retryOnThisLockResult.Handle;
                    case MultiplexedConnectionLockRetry.Retry:
                        break;
                    default:
                        throw new InvalidOperationException("unexpected retry");
                }
            }
            finally
            {
                // since we took this lock from the pool, always return it to the pool
                await this.StoreOrDisposeLockAsync(connectionSource, existingLock, shouldDispose: canSafelyDisposeExistingLock).ConfigureAwait(false);
            }
        }

        // normal phase: if we were not able to be opportunistic, ensure that we have a lock
        var @lock = new MultiplexedConnectionLock(this.ConnectionFactory(connectionSource));
        MultiplexedConnectionLock.Result? result = null;
        try
        {
            result = await TryAcquireAsync(@lock, opportunistic: false).ConfigureAwait(false);
            Invariant.Require(result!.Value.Retry == MultiplexedConnectionLockRetry.NoRetry, "Acquire on fresh lock should not recommend a retry");
        }
        finally
        {
            // if we failed to even acquire a result on a brand new lock, then there's definitely no reason to store it
            await this.StoreOrDisposeLockAsync(connectionSource, @lock, shouldDispose: result?.CanSafelyDispose ?? true).ConfigureAwait(false);
        }
        return result.Value.Handle;

        ValueTask<MultiplexedConnectionLock.Result> TryAcquireAsync(MultiplexedConnectionLock @lock, bool opportunistic) =>
            @lock.TryAcquireAsync(name, timeout, strategy, keepaliveCadence, cancellationToken, opportunistic);
    }

    private async ValueTask<MultiplexedConnectionLock?> GetExistingLockOrDefaultAsync(TConnectionSource connectionSource)
    {
        using var _ = await this._lock.AcquireAsync(CancellationToken.None).ConfigureAwait(false);

        if (this._poolsByConnectionSource.TryGetValue(connectionSource, out var pool) && pool.Count != 0)
        {
            --this._pooledLockCount;
            return pool.Dequeue();
        }

        return null;
    }

    private async ValueTask StoreOrDisposeLockAsync(TConnectionSource connectionSource, MultiplexedConnectionLock @lock, bool shouldDispose)
    {
        if (shouldDispose)
        {
            try { await @lock.DisposeAsync().ConfigureAwait(false); }
            catch { /* swallow */ }
        }

        using (await this._lock.AcquireAsync(CancellationToken.None).ConfigureAwait(false))
        {
            ++this._storeCountSinceLastPrune;

            if (shouldDispose)
            {
                // If we're about to dispose the lock, check if it has an empty pool that can be removed from our dictionary.
                // By itself this doesn't guarantee cleanup: after a successful acquire we'll have an empty lock left over that won't
                // go away unless we use THAT connection source again. To help with this, we have pruning
                if (this._poolsByConnectionSource.TryGetValue(connectionSource, out var pool) && pool.Count == 0)
                {
                    this._poolsByConnectionSource.Remove(connectionSource);
                }
            }
            else // otherwise, store the lock
            {
                ++this._pooledLockCount;

                if (this._poolsByConnectionSource.TryGetValue(connectionSource, out var existing))
                {
                    existing.Enqueue(@lock);
                }
                else
                {
                    var newPool = new Queue<MultiplexedConnectionLock>();
                    newPool.Enqueue(@lock);
                    this._poolsByConnectionSource.Add(connectionSource, newPool);
                }
            }

            if (this.IsDueForPruningNoLock())
            {
                await this.PrunePoolsNoLockAsync().ConfigureAwait(false);
            }
        }
    }

    private bool IsDueForPruningNoLock()
    {
        // Since pruning is expensive, we want to amortize its cost across many operations. The idea here is
        // that each StoreOrDisposeLockAsync() call gives us one "ticket" that we can cache in later to justify
        // some pruning work. The cost to prune is equal to the number of queues to scan plus the total number of
        // items in each queue. Therefore we prune when we've built up enough tickets to "pay for" a pruning operation.
        // The whole reason to prune is to avoid memory bloat (connection bloat isn't an issue since we only keep connections
        // open when needed). So, we don't even consider pruning below a certain storage threshold

        var pruningCost = this._pooledLockCount + this._poolsByConnectionSource.Count;
        return pruningCost > 64 && this._storeCountSinceLastPrune >= pruningCost;
    }

    private async ValueTask PrunePoolsNoLockAsync()
    {
        this._storeCountSinceLastPrune = 0; // reset

        List<TConnectionSource>? connectionSourcesToRemove = null;
        foreach (var kvp in this._poolsByConnectionSource)
        {
            var pool = kvp.Value;
            MultiplexedConnectionLock? firstRetainedLock = null;
            while (pool.Count != 0 && pool.Peek() != firstRetainedLock)
            {
                var @lock = pool.Dequeue();
                if (await @lock.GetIsInUseAsync().ConfigureAwait(false))
                {
                    firstRetainedLock ??= @lock;
                    pool.Enqueue(@lock);
                }
                else
                {
                    --this._pooledLockCount;
                    try { await @lock.DisposeAsync().ConfigureAwait(false); }
                    catch { /* swallow */ }
                }
            }

            if (pool.Count == 0)
            {
                (connectionSourcesToRemove ??= new List<TConnectionSource>()).Add(kvp.Key);
            }
        }

        if (connectionSourcesToRemove != null)
        {
            foreach (var connectionSourceToRemove in connectionSourcesToRemove)
            {
                this._poolsByConnectionSource.Remove(connectionSourceToRemove);
            }
        }
    }
}
