using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using System.Data;

namespace Medallion.Threading.SqlServer;

/// <summary>
/// Implements a distributed semaphore using SQL Server constructs.
/// </summary>
public sealed partial class SqlDistributedSemaphore : IInternalDistributedSemaphore<SqlDistributedSemaphoreHandle>
{
    private readonly IDbDistributedLock _internalLock;
    private readonly SqlSemaphore _strategy;

    #region ---- Constructors ----
    /// <summary>
    /// Creates a semaphore with name <paramref name="name"/> that can be acquired up to <paramref name="maxCount"/> 
    /// times concurrently. The provided <paramref name="connectionString"/> will be used to connect to the database.
    /// </summary>
    public SqlDistributedSemaphore(string name, int maxCount, string connectionString, Action<SqlConnectionOptionsBuilder>? options = null)
        : this(name, maxCount, n => SqlDistributedLock.CreateInternalLock(n, connectionString, options))
    {
    }

    /// <summary>
    /// Creates a semaphore with name <paramref name="name"/> that can be acquired up to <paramref name="maxCount"/> 
    /// times concurrently. When acquired, the semaphore will be scoped to the given <paramref name="connection"/>. 
    /// The <paramref name="connection"/> is assumed to be externally managed: the <see cref="SqlDistributedSemaphore"/> will 
    /// not attempt to open, close, or dispose it
    /// </summary>
    public SqlDistributedSemaphore(string name, int maxCount, IDbConnection connection)
        : this(name, maxCount, n => SqlDistributedLock.CreateInternalLock(n, connection))
    {
    }

    /// <summary>
    /// Creates a semaphore with name <paramref name="name"/> that can be acquired up to <paramref name="maxCount"/> 
    /// times concurrently. When acquired, the semaphore will be scoped to the given <paramref name="transaction"/>. 
    /// The <paramref name="transaction"/> and its <see cref="IDbTransaction.Connection"/> are assumed to be externally managed: 
    /// the <see cref="SqlDistributedSemaphore"/> will not attempt to open, close, commit, roll back, or dispose them
    /// </summary>
    public SqlDistributedSemaphore(string name, int maxCount, IDbTransaction transaction)
        : this(name, maxCount, n => SqlDistributedLock.CreateInternalLock(n, transaction))
    {
    }

    private SqlDistributedSemaphore(string name, int maxCount, Func<string, IDbDistributedLock> createInternalLockFromName)
    {
        if (maxCount < 1) { throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "must be positive"); }

        this.Name = name ?? throw new ArgumentNullException(nameof(name));
        this._strategy = new SqlSemaphore(maxCount);
        this._internalLock = createInternalLockFromName(SqlSemaphore.ToSafeName(name));
    }
    #endregion

    /// <summary>
    /// Implements <see cref="IDistributedSemaphore.Name"/>
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Implements <see cref="IDistributedSemaphore.MaxCount"/>
    /// </summary>
    public int MaxCount => this._strategy.MaxCount;

    async ValueTask<SqlDistributedSemaphoreHandle?> IInternalDistributedSemaphore<SqlDistributedSemaphoreHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        var handle = await this._internalLock.TryAcquireAsync(timeout, this._strategy, cancellationToken, contextHandle: null).ConfigureAwait(false);
        return handle != null ? new SqlDistributedSemaphoreHandle(handle) : null;
    }
}
