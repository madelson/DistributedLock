using Medallion.Threading.Internal;

namespace Medallion.Threading.GBase;

/// <summary>
/// Specifies options for connecting to and locking against an GBase database
/// </summary>
public sealed class GBaseConnectionOptionsBuilder
{
    private TimeoutValue? _keepaliveCadence;
    private bool? _useMultiplexing;

    internal GBaseConnectionOptionsBuilder() { }

    /// <summary>
    /// GBase does not kill idle connections by default, so by default keepalive is disabled (set to <see cref="Timeout.InfiniteTimeSpan"/>).
    /// 
    /// However, if you are using the IDLE_TIME setting in GBase or if your network is dropping connections that are idle holding locks for
    /// a long time, you can set a value for keepalive to prevent this from happening.
    /// 
    /// </summary>
    public GBaseConnectionOptionsBuilder KeepaliveCadence(TimeSpan keepaliveCadence)
    {
        this._keepaliveCadence = new TimeoutValue(keepaliveCadence, nameof(keepaliveCadence));
        return this;
    }

    /// <summary>
    /// This mode takes advantage of the fact that while "holding" a lock (or other synchronization primitive)
    /// a connection is essentially idle. Thus, rather than creating a new connection for each held lock it is 
    /// often possible to multiplex a shared connection so that that connection can hold multiple locks at the same time.
    /// 
    /// Multiplexing is on by default.
    /// 
    /// This is implemented in such a way that releasing a lock held on such a connection will never be blocked by an
    /// Acquire() call that is waiting to acquire a lock on that same connection. For this reason, the multiplexing
    /// strategy is "optimistic": if the lock can't be acquired instantaneously on the shared connection, a new (shareable) 
    /// connection will be allocated.
    /// 
    /// This option can improve performance and avoid connection pool starvation in high-load scenarios. It is also
    /// particularly applicable to cases where <see cref="IDistributedLock.TryAcquire(TimeSpan, System.Threading.CancellationToken)"/>
    /// semantics are used with a zero-length timeout.
    /// </summary>
    public GBaseConnectionOptionsBuilder UseMultiplexing(bool useMultiplexing = true)
    {
        this._useMultiplexing = useMultiplexing;
        return this;
    }

    internal static (TimeoutValue keepaliveCadence, bool useMultiplexing) GetOptions(Action<GBaseConnectionOptionsBuilder>? optionsBuilder)
    {
        GBaseConnectionOptionsBuilder? options;
        if (optionsBuilder != null)
        {
            options = new GBaseConnectionOptionsBuilder();
            optionsBuilder(options);
        }
        else
        {
            options = null;
        }

        var keepaliveCadence = options?._keepaliveCadence ?? Timeout.InfiniteTimeSpan;
        var useMultiplexing = options?._useMultiplexing ?? true;

        return (keepaliveCadence, useMultiplexing);
    }
}
