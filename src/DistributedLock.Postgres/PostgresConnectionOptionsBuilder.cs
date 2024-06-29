using Medallion.Threading.Internal;
using System.Data;

namespace Medallion.Threading.Postgres;

/// <summary>
/// Specifies options for connecting to and locking against a Postgres database
/// </summary>
public sealed class PostgresConnectionOptionsBuilder
{
    private TimeoutValue? _keepaliveCadence;
    private bool? _useTransaction, _useMultiplexing;

    internal PostgresConnectionOptionsBuilder() { }

    /// <summary>
    /// Some Postgres setups have automation in place which aggressively kills idle connections.
    ///
    /// To prevent this, this option sets the cadence at which we run a no-op "keepalive" query on a connection that is holding a lock.
    /// Note that this still does not guarantee protection for the connection from all conditions where the governor might kill it.
    ///
    /// Defaults to <see cref="Timeout.InfiniteTimeSpan"/>, which disables keepalive.
    /// </summary>
    public PostgresConnectionOptionsBuilder KeepaliveCadence(TimeSpan keepaliveCadence)
    {
        this._keepaliveCadence = new TimeoutValue(keepaliveCadence, nameof(keepaliveCadence));
        return this;
    }

    /// <summary>
    /// Whether the synchronization should use a transaction scope rather than a session scope. Defaults to false.
    ///
    /// Synchronizing based on a transaction is necessary to do distributed locking with some pgbouncer configurations
    /// (see https://github.com/madelson/DistributedLock/issues/168#issuecomment-1823277173). It may also be marginally less
    /// expensive than using a connection for a single lock because releasing requires only disposing the
    /// underlying <see cref="IDbTransaction"/>.
    ///
    /// The disadvantage of this strategy is that it is incompatible with <see cref="UseMultiplexing(bool)"/> and therefore
    /// gives up the advantages of that approach.
    /// </summary>
    public PostgresConnectionOptionsBuilder UseTransaction(bool useTransaction = true)
    {
        this._useTransaction = useTransaction;
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
    public PostgresConnectionOptionsBuilder UseMultiplexing(bool useMultiplexing = true)
    {
        this._useMultiplexing = useMultiplexing;
        return this;
    }

    internal static (TimeoutValue keepaliveCadence, bool useTransaction, bool useMultiplexing) GetOptions(
        Action<PostgresConnectionOptionsBuilder>? optionsBuilder)
    {
        PostgresConnectionOptionsBuilder? options;
        if (optionsBuilder != null)
        {
            options = new();
            optionsBuilder(options);
        }
        else
        {
            options = null;
        }

        var keepaliveCadence = options?._keepaliveCadence ?? Timeout.InfiniteTimeSpan;
        var useTransaction = options?._useTransaction ?? false;
        var useMultiplexing = options?._useMultiplexing ?? !options?._useTransaction ?? true;

        if (useMultiplexing && useTransaction)
        {
            throw new ArgumentException(nameof(UseTransaction) + ": is not compatible with " + nameof(UseMultiplexing));
        }

        return (keepaliveCadence, useTransaction, useMultiplexing);
    }
}