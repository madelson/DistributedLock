using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    /// <summary>
    /// Implements a distributed lock using a SQL server application lock
    /// (see https://msdn.microsoft.com/en-us/library/ms189823.aspx)
    /// </summary>
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public sealed class SqlDistributedLocks : IDistributedLock
    {
        private readonly IInternalSqlDistributedLocks internalLocks;

        /// <summary>
        /// Creates a lock with name <paramref name="lockNames"/>, using the given
        /// <paramref name="connectionString"/> to connect to the database.
        /// 
        /// Uses <see cref="SqlDistributedLockConnectionStrategy.Default"/>
        /// </summary>
        public SqlDistributedLocks(IEnumerable<string> lockNames, string connectionString)
            : this(lockNames, connectionString, SqlDistributedLockConnectionStrategy.Default)
        {
        }

        /// <summary>
        /// Creates a lock with name <paramref name="lockNames"/>, using the given
        /// <paramref name="connectionString"/> to connect to the database via the strategy
        /// specified by <paramref name="connectionStrategy"/>
        /// </summary>
        public SqlDistributedLocks(IEnumerable<string> lockNames, string connectionString, SqlDistributedLockConnectionStrategy connectionStrategy)
            : this(lockNames, CreateInternalLock(lockNames, connectionString, connectionStrategy))
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException("connectionString");
        }

        /// <summary>
        /// Creates a lock with name <paramref name="lockNames"/> which, when acquired,
        /// will be scoped to the given <paramref name="connection"/>. The <paramref name="connection"/> is
        /// assumed to be externally managed: the <see cref="SqlDistributedLock"/> will not attempt to open,
        /// close, or dispose it
        /// </summary>
        public SqlDistributedLocks(IEnumerable<string> lockNames, DbConnection connection)
            : this(lockNames, (IDbConnection)connection)
        {
        }

        /// <summary>
        /// Creates a lock with name <paramref name="lockNames"/> which, when acquired,
        /// will be scoped to the given <paramref name="transaction"/>. The <paramref name="transaction"/> and its
        /// <see cref="DbTransaction.Connection"/> are assumed to be externally managed: the <see cref="SqlDistributedLock"/> will 
        /// not attempt to open, close, commit, roll back, or dispose them
        /// </summary>
        public SqlDistributedLocks(IEnumerable<string> lockNames, DbTransaction transaction)
            : this(lockNames, (IDbTransaction)transaction)
        {
        }

        /// <summary>
        /// Creates a lock with names <paramref name="lockNames"/> which, when acquired,
        /// will be scoped to the given <paramref name="connection"/>. The <paramref name="connection"/> is
        /// assumed to be externally managed: the <see cref="SqlDistributedLock"/> will not attempt to open,
        /// close, or dispose it
        /// </summary>
        public SqlDistributedLocks(IEnumerable<string> lockNames, IDbConnection connection)
            : this(lockNames, new ExternalConnectionOrTransactionSqlDistributedLocks(lockNames, new ConnectionOrTransaction(connection ?? throw new ArgumentNullException(nameof(connection)))))
        {
        }

        /// <summary>
        /// Creates a lock with name <paramref name="lockNames"/> which, when acquired,
        /// will be scoped to the given <paramref name="transaction"/>. The <paramref name="transaction"/> and its
        /// <see cref="DbTransaction.Connection"/> are assumed to be externally managed: the <see cref="SqlDistributedLock"/> will 
        /// not attempt to open, close, commit, roll back, or dispose them
        /// </summary>
        public SqlDistributedLocks(IEnumerable<string> lockNames, IDbTransaction transaction)
            : this(lockNames, new ExternalConnectionOrTransactionSqlDistributedLocks(lockNames, new ConnectionOrTransaction(transaction ?? throw new ArgumentNullException(nameof(transaction)))))
        {
        }

        private SqlDistributedLocks(IEnumerable<string> lockNames, IInternalSqlDistributedLocks internalLocks)
        {
            if (lockNames == null)
                throw new ArgumentNullException(nameof(lockNames));
            if (lockNames.Any(lck => lck.Length > MaxLockNameLength))
                throw new FormatException("lockName: must be at most " + MaxLockNameLength + " characters");

            this.internalLocks = internalLocks;
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
        public IDisposable? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default)
        {
            return cancellationToken.CanBeCanceled
                // use the async version since that supports cancellation
                ? DistributedLockHelpers.TryAcquireWithAsyncCancellation(this, timeout, cancellationToken)
                // synchronous mode
                : this.internalLocks.TryAcquire(timeout.ToInt32Timeout(), SqlApplicationLocks.ExclusiveLock, contextHandle: null);
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
        public IDisposable Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            return DistributedLockHelpers.Acquire(this, timeout, cancellationToken);
        }

        public Task<IDisposable?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
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
        public Task<IDisposable> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
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

        internal static IInternalSqlDistributedLocks CreateInternalLock(IEnumerable<string> lockNames, string connectionString, SqlDistributedLockConnectionStrategy connectionStrategy)
        {
            switch (connectionStrategy) 
            {
                case SqlDistributedLockConnectionStrategy.Default:
                case SqlDistributedLockConnectionStrategy.Connection:
                    return new OwnedConnectionSqlDistributedLocks(lockNames, connectionString: connectionString);
                case SqlDistributedLockConnectionStrategy.Transaction:
                    return new OwnedTransactionSqlDistributedLocks(lockNames, connectionString: connectionString);
                /*case SqlDistributedLockConnectionStrategy.OptimisticConnectionMultiplexing:
                    return new OptimisticConnectionMultiplexingSqlDistributedLock(lockNames, connectionString: connectionString);
                case SqlDistributedLockConnectionStrategy.Azure:
                    return new AzureSqlDistributedLock(lockNames, connectionString: connectionString);*/
                default:
                    throw new ArgumentException(nameof(connectionStrategy));
            }
        }
    }
}
