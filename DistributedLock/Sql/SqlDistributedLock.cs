using Medallion.Threading.Sql.ConnectionMultiplexing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    /// <summary>
    /// Implements a distributed lock using a SQL server application lock
    /// (see https://msdn.microsoft.com/en-us/library/ms189823.aspx)
    /// </summary>
    public sealed class SqlDistributedLock : IDistributedLock
    {
        private readonly IInternalSqlDistributedLock internalLock;

        /// <summary>
        /// Creates a lock with name <paramref name="lockName"/>, using the given
        /// <paramref name="connectionString"/> to connect to the database.
        /// 
        /// Uses <see cref="SqlDistributedLockConnectionStrategy.Default"/>
        /// </summary>
        public SqlDistributedLock(string lockName, string connectionString)
            : this(lockName, connectionString, SqlDistributedLockConnectionStrategy.Default)
        {
        }

        /// <summary>
        /// Creates a lock with name <paramref name="lockName"/>, using the given
        /// <paramref name="connectionString"/> to connect to the database via the strategy
        /// specified by <paramref name="connectionStrategy"/>
        /// </summary>
        public SqlDistributedLock(string lockName, string connectionString, SqlDistributedLockConnectionStrategy connectionStrategy)
            : this(lockName, CreateInternalLock(lockName, connectionString, connectionStrategy))
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException("connectionString");
        }

        /// <summary>
        /// Creates a lock with name <paramref name="lockName"/> which, when acquired,
        /// will be scoped to the given <paramref name="connection"/>. The <paramref name="connection"/> is
        /// assumed to be externally managed: the <see cref="SqlDistributedLock"/> will not attempt to open,
        /// close, or dispose it
        /// </summary>
        public SqlDistributedLock(string lockName, DbConnection connection)
            : this(lockName, (IDbConnection)connection)
        {
        }

        /// <summary>
        /// Creates a lock with name <paramref name="lockName"/> which, when acquired,
        /// will be scoped to the given <paramref name="transaction"/>. The <paramref name="transaction"/> and its
        /// <see cref="DbTransaction.Connection"/> are assumed to be externally managed: the <see cref="SqlDistributedLock"/> will 
        /// not attempt to open, close, commit, roll back, or dispose them
        /// </summary>
        public SqlDistributedLock(string lockName, DbTransaction transaction)
            : this(lockName, (IDbTransaction)transaction)
        {
        }

        /// <summary>
        /// Creates a lock with name <paramref name="lockName"/> which, when acquired,
        /// will be scoped to the given <paramref name="connection"/>. The <paramref name="connection"/> is
        /// assumed to be externally managed: the <see cref="SqlDistributedLock"/> will not attempt to open,
        /// close, or dispose it
        /// </summary>
        public SqlDistributedLock(string lockName, IDbConnection connection)
            : this(lockName, new ConnectionScopedSqlDistributedLock(lockName, connection))
        {
            if (connection == null)
                throw new ArgumentNullException("connection");
        }

        /// <summary>
        /// Creates a lock with name <paramref name="lockName"/> which, when acquired,
        /// will be scoped to the given <paramref name="transaction"/>. The <paramref name="transaction"/> and its
        /// <see cref="DbTransaction.Connection"/> are assumed to be externally managed: the <see cref="SqlDistributedLock"/> will 
        /// not attempt to open, close, commit, roll back, or dispose them
        /// </summary>
        public SqlDistributedLock(string lockName, IDbTransaction transaction)
            : this(lockName, new TransactionScopedSqlDistributedLock(lockName, transaction))
        {
            if (transaction == null)
                throw new ArgumentNullException("transaction");
        }

        private SqlDistributedLock(string lockName, IInternalSqlDistributedLock internalLock)
        {
            if (lockName == null)
                throw new ArgumentNullException("lockName");
            if (lockName.Length > MaxLockNameLength)
                throw new FormatException("lockName: must be at most " + MaxLockNameLength + " characters");

            this.internalLock = internalLock;
        }

        #region ---- Public API ----
        /// <summary>
        /// Attempts to acquire the lock synchronously. Usage:
        /// <code>
        ///     using (var handle = myLock.TryAcquire(...))
        ///     {
        ///         if (handle != null) { /* we have the lock! */ }
        ///     }
        ///     // dispose releases the lock if we took it
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on acquiring the lock. Defaults to 0</param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>An <see cref="IDisposable"/> "handle" which can be used to release the lock, or null if the lock was not taken</returns>
        public IDisposable TryAcquire(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            return cancellationToken.CanBeCanceled
                // use the async version since that supports cancellation
                ? DistributedLockHelpers.TryAcquireWithAsyncCancellation(this, timeout, cancellationToken)
                // synchronous mode
                : this.internalLock.TryAcquire(timeout.ToInt32Timeout(), SqlApplicationLock.Mode.Exclusive, contextHandle: null);
        }

        /// <summary>
        /// Acquires the lock synchronously, failing with <see cref="TimeoutException"/> if the wait times out
        /// <code>
        ///     using (myLock.Acquire(...))
        ///     {
        ///         // we have the lock
        ///     }
        ///     // dispose releases the lock
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on acquiring the lock. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>An <see cref="IDisposable"/> "handle" which can be used to release the lock</returns>
        public IDisposable Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DistributedLockHelpers.Acquire(this, timeout, cancellationToken);
        }

        /// <summary>
        /// Attempts to acquire the lock asynchronously. Usage:
        /// <code>
        ///     using (var handle = await myLock.TryAcquireAsync(...))
        ///     {
        ///         if (handle != null) { /* we have the lock! */ }
        ///     }
        ///     // dispose releases the lock if we took it
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on acquiring the lock. Defaults to 0</param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>An <see cref="IDisposable"/> "handle" which can be used to release the lock, or null if the lock was not taken</returns>
        public Task<IDisposable> TryAcquireAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.internalLock.TryAcquireAsync(timeout.ToInt32Timeout(), SqlApplicationLock.Mode.Exclusive, cancellationToken, contextHandle: null);
        }

        /// <summary>
        /// Acquires the lock asynchronously, failing with <see cref="TimeoutException"/> if the wait times out
        /// <code>
        ///     using (await myLock.AcquireAsync(...))
        ///     {
        ///         // we have the lock
        ///     }
        ///     // dispose releases the lock
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on acquiring the lock. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>An <see cref="IDisposable"/> "handle" which can be used to release the lock</returns>
        public Task<IDisposable> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DistributedLockHelpers.AcquireAsync(this, timeout, cancellationToken);
        }

        /// <summary>
        /// The maximum allowed length for lock names. See https://msdn.microsoft.com/en-us/library/ms189823.aspx
        /// </summary>
        public static int MaxLockNameLength => 255;

        /// <summary>
        /// Given <paramref name="baseLockName"/>, constructs a lock name which is safe for use with <see cref="SqlDistributedLock"/>
        /// </summary>
        public static string GetSafeLockName(string baseLockName)
        {
            return DistributedLockHelpers.ToSafeLockName(baseLockName, MaxLockNameLength, s => s);
        }
        #endregion

        internal static IInternalSqlDistributedLock CreateInternalLock(string lockName, string connectionString, SqlDistributedLockConnectionStrategy connectionStrategy)
        {
            switch (connectionStrategy) 
            {
                case SqlDistributedLockConnectionStrategy.Default:
                case SqlDistributedLockConnectionStrategy.Connection:
                    return new OwnedConnectionDistributedLock(lockName: lockName, connectionString: connectionString);
                case SqlDistributedLockConnectionStrategy.Transaction:
                    return new OwnedTransactionSqlDistributedLock(lockName: lockName, connectionString: connectionString);
                case SqlDistributedLockConnectionStrategy.OptimisticConnectionMultiplexing:
                    return new OptimisticConnectionMultiplexingSqlDistributedLock(lockName: lockName, connectionString: connectionString);
                default:
                    throw new ArgumentException(nameof(connectionStrategy));
            }
        }
    }
}
