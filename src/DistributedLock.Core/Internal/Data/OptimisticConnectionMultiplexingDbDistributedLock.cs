namespace Medallion.Threading.Internal.Data;

/// <summary>
/// Implements <see cref="IDbDistributedLock"/> by multiplexing across connections where possible
/// </summary>
#if DEBUG
public
#else
internal 
#endif
sealed class OptimisticConnectionMultiplexingDbDistributedLock<TConnectionSource> : IDbDistributedLock
    where TConnectionSource : notnull
{
    private readonly string _name;
    private readonly TConnectionSource _connectionSource;
    private readonly MultiplexedConnectionLockPool<TConnectionSource> _multiplexedConnectionLockPool;
    private readonly TimeoutValue _keepaliveCadence;
    private readonly IDbDistributedLock _fallbackLock;

    public OptimisticConnectionMultiplexingDbDistributedLock(
        string name,
        TConnectionSource connectionSource,
        MultiplexedConnectionLockPool<TConnectionSource> multiplexedConnectionLockPool,
        TimeoutValue keepaliveCadence)
    {
        this._name = name;
        this._connectionSource = connectionSource;
        this._multiplexedConnectionLockPool = multiplexedConnectionLockPool;
        this._keepaliveCadence = keepaliveCadence;
        this._fallbackLock = new DedicatedConnectionOrTransactionDbDistributedLock(
            name,
            () => this._multiplexedConnectionLockPool.ConnectionFactory(this._connectionSource),
            useTransaction: false,
            keepaliveCadence: keepaliveCadence
        );
    }

    public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync<TLockCookie>(
        TimeoutValue timeout, 
        IDbSynchronizationStrategy<TLockCookie> strategy, 
        CancellationToken cancellationToken, 
        IDistributedSynchronizationHandle? contextHandle)
        where TLockCookie : class
    {
        // cannot multiplex for updates, since we cannot predict whether or not there will be a request to elevate
        // to an exclusive lock which asks for a long timeout
        if (!strategy.IsUpgradeable && contextHandle == null)
        {
            return this._multiplexedConnectionLockPool.TryAcquireAsync(this._connectionSource, this._name, timeout, strategy, keepaliveCadence: this._keepaliveCadence, cancellationToken);
        }

        // otherwise, fall back to our fallback lock
        return this._fallbackLock.TryAcquireAsync(timeout, strategy, cancellationToken, contextHandle);
    }
}
