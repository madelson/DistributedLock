using Medallion.Threading.Internal;
using System.Data;

namespace Medallion.Threading.SqlServer;

/// <summary>
/// Specifies options for connecting to and locking against a SQL database
/// </summary>
public sealed class SqlConnectionOptionsBuilder
{
    private TimeoutValue? _keepaliveCadence;
    private bool? _useTransaction, _useMultiplexing;

    internal SqlConnectionOptionsBuilder() { }

    /// <summary>
    /// Using SQL Azure as a distributed synchronization provider can be challenging due to Azure's aggressive connection governor
    /// which proactively kills idle connections. 
    /// 
    /// To prevent this, this option sets the cadence at which we run a no-op "keepalive" query on a connection that is holding a lock. 
    /// Note that this still does not guarantee protection for the connection from all conditions where the governor might kill it.
    /// 
    /// To disable keepalive, set to <see cref="Timeout.InfiniteTimeSpan"/>.
    /// 
    /// Defaults to 10 minutes based on Azure's 30 minute default behavior.
    /// 
    /// For more information, see the dicussion on https://github.com/madelson/DistributedLock/issues/5
    /// </summary>
    public SqlConnectionOptionsBuilder KeepaliveCadence(TimeSpan keepaliveCadence)
    {
        this._keepaliveCadence = new TimeoutValue(keepaliveCadence, nameof(keepaliveCadence));
        return this;
    }

    /// <summary>
    /// Whether the synchronization should use a transaction scope rather than a session scope. Defaults to false.
    /// 
    /// Synchronizing based on a transaction is marginally less expensive than using a connection
    /// because releasing requires only disposing the underlying <see cref="IDbTransaction"/>.
    /// The disadvantage is that using this strategy may lead to long-running transactions, which can be
    /// problematic for databases using the full recovery model.
    /// </summary>
    public SqlConnectionOptionsBuilder UseTransaction(bool useTransaction = true)
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
    public SqlConnectionOptionsBuilder UseMultiplexing(bool useMultiplexing = true)
    {
        this._useMultiplexing = useMultiplexing;
        return this;
    }

    internal static (TimeoutValue keepaliveCadence, bool useTransaction, bool useMultiplexing) GetOptions(Action<SqlConnectionOptionsBuilder>? optionsBuilder)
    {
        SqlConnectionOptionsBuilder? options;
        if (optionsBuilder != null)
        {
            options = new SqlConnectionOptionsBuilder();
            optionsBuilder(options);
        }
        else
        {
            options = null;
        }

        var keepaliveCadence = options?._keepaliveCadence ?? TimeSpan.FromMinutes(10);
        var useTransaction = options?._useTransaction ?? false;
        var useMultiplexing = options?._useMultiplexing ?? true;

        if (useMultiplexing && useTransaction)
        {
            throw new ArgumentException(nameof(UseTransaction) + ": is not compatible with " + nameof(UseMultiplexing));
        }

        return (keepaliveCadence, useTransaction, useMultiplexing);
    }
}
