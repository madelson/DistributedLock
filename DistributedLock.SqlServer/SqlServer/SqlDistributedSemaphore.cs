using Medallion.Threading.Data;
using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.SqlServer
{
    /// <summary>
    /// Implements a distributed semaphore using SQL Server constructs.
    /// </summary>
    public class SqlDistributedSemaphore
    {
        private readonly IInternalSqlDistributedLock _internalLock;
        private readonly SqlSemaphore _strategy;

        #region ---- Constructors ----
        /// <summary>
        /// Creates a semaphore with name <paramref name="name"/> that can be acquired up to <paramref name="maxCount"/> 
        /// times concurrently. Uses the given <paramref name="connectionString"/> to connect to the database.
        /// 
        /// Uses <see cref="SqlDistributedLockConnectionStrategy.Default"/>
        /// </summary>
        public SqlDistributedSemaphore(string name, int maxCount, string connectionString)
            : this(name, maxCount, connectionString, SqlDistributedLockConnectionStrategy.Default)
        {
        }

        /// <summary>
        /// Creates a semaphore with name <paramref name="name"/> that can be acquired up to <paramref name="maxCount"/> 
        /// times concurrently. Uses the given <paramref name="connectionString"/> to connect to the database via the strategy
        /// specified by <paramref name="connectionStrategy"/>
        /// </summary>
        public SqlDistributedSemaphore(string name, int maxCount, string connectionString, SqlDistributedLockConnectionStrategy connectionStrategy)
            : this(name, maxCount, name => SqlDistributedLock.CreateInternalLock(name, connectionString, connectionStrategy))
        {
        }

        /// <summary>
        /// Creates a semaphore with name <paramref name="name"/> that can be acquired up to <paramref name="maxCount"/> 
        /// times concurrently. When acquired, the semaphore will be scoped to the given <paramref name="connection"/>. 
        /// The <paramref name="connection"/> is assumed to be externally managed: the <see cref="SqlDistributedSemaphore"/> will 
        /// not attempt to open, close, or dispose it
        /// </summary>
        public SqlDistributedSemaphore(string name, int maxCount, IDbConnection connection)
            : this(name, maxCount, name => new ExternalConnectionOrTransactionSqlDistributedLock(name, new ConnectionOrTransaction(connection ?? throw new ArgumentNullException(nameof(connection)))))
        {
        }

        /// <summary>
        /// Creates a semaphore with name <paramref name="name"/> that can be acquired up to <paramref name="maxCount"/> 
        /// times concurrently. When acquired, the semaphore will be scoped to the given <paramref name="transaction"/>. 
        /// The <paramref name="transaction"/> and its <see cref="IDbTransaction.Connection"/> are assumed to be externally managed: 
        /// the <see cref="SqlDistributedSemaphore"/> will not attempt to open, close, commit, roll back, or dispose them
        /// </summary>
        public SqlDistributedSemaphore(string name, int maxCount, IDbTransaction transaction)
            : this(name, maxCount, name => new ExternalConnectionOrTransactionSqlDistributedLock(name, new ConnectionOrTransaction(transaction ?? throw new ArgumentNullException(nameof(transaction)))))
        {
        }

        private SqlDistributedSemaphore(string name, int maxCount, Func<string, IInternalSqlDistributedLock> createInternalLockFromName)
        {
            if (name == null) { throw new ArgumentNullException(nameof(name)); }
            if (maxCount < 1) { throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "must be positive"); }

            this.Name = name;
            this._strategy = new SqlSemaphore(maxCount);
            this._internalLock = createInternalLockFromName(SqlSemaphore.ToSafeName(name));
        }
        #endregion

        public string Name { get; }

        /// <summary>
        /// Attempts to acquire a semaphore ticket synchronously. Usage: 
        /// <code>
        ///     using (var handle = mySemaphore.TryAcquire(...))
        ///     {
        ///         if (handle != null) { /* we have the ticket! */ }
        ///     }
        ///     // dispose releases the ticket if we took it
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to 0</param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>A <see cref="SqlDistributedSemaphoreHandle"/> which can be used to release the ticket or null on failure</returns>
        public SqlDistributedSemaphoreHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            SyncOverAsync.Run(t => t.@this.TryAcquireAsync(t.timeout, t.cancellationToken), (@this: this, timeout, cancellationToken), false);

        /// <summary>
        /// Acquires a semaphore ticket synchronously, failing with <see cref="TimeoutException"/> if the attempt times out. Usage: 
        /// <code>
        ///     using (mySemaphore.Acquire(...))
        ///     {
        ///         /* we have the ticket! */
        ///     }
        ///     // dispose releases the ticket
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>A <see cref="SqlDistributedSemaphoreHandle"/> which can be used to release the ticket</returns>
        public SqlDistributedSemaphoreHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            SyncOverAsync.Run(t => t.@this.AcquireAsync(t.timeout, t.cancellationToken), (@this: this, timeout, cancellationToken), false);

        /// <summary>
        /// Attempts to acquire a semaphore ticket asynchronously. Usage: 
        /// <code>
        ///     await using (var handle = await mySemaphore.TryAcquireAsync(...))
        ///     {
        ///         if (handle != null) { /* we have the ticket! */ }
        ///     }
        ///     // dispose releases the ticket if we took it
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to 0</param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>A <see cref="SqlDistributedSemaphoreHandle"/> which can be used to release the ticket or null on failure</returns>
        public ValueTask<SqlDistributedSemaphoreHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            this.TryAcquireInternalAsync(timeout, cancellationToken);

        /// <summary>
        /// Acquires a semaphore ticket asynchronously, failing with <see cref="TimeoutException"/> if the attempt times out. Usage: 
        /// <code>
        ///     await using (await mySemaphore.AcquireAsync(...))
        ///     {
        ///         /* we have the ticket! */
        ///     }
        ///     // dispose releases the ticket
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>A <see cref="SqlDistributedSemaphoreHandle"/> which can be used to release the ticket</returns>
        public ValueTask<SqlDistributedSemaphoreHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            this.TryAcquireInternalAsync(timeout, cancellationToken).ThrowTimeoutIfNull();

        private async ValueTask<SqlDistributedSemaphoreHandle?> TryAcquireInternalAsync(TimeoutValue timeout, CancellationToken cancellationToken)
        {
            var handle = await this._internalLock.TryAcquireAsync(timeout, this._strategy, cancellationToken, contextHandle: null).ConfigureAwait(false);
            return handle != null ? new SqlDistributedSemaphoreHandle(handle) : null;
        }
    }
}
