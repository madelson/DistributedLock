using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    /// <summary>
    /// Implements reader-writer lock semantics using a SQL server application lock
    /// (see https://msdn.microsoft.com/en-us/library/ms189823.aspx).
    /// 
    /// This class supports the following patterns:
    /// * Multiple readers AND single writer (using <see cref="AcquireReadLock(TimeSpan?, CancellationToken)"/> and <see cref="AcquireUpgradeableReadLock(TimeSpan?, CancellationToken)"/>)
    /// * Multiple readers OR single writer (using <see cref="AcquireReadLock(TimeSpan?, CancellationToken)"/> and <see cref="AcquireWriteLock(TimeSpan?, CancellationToken)"/>)
    /// * Upgradeable read locks similar to <see cref="ReaderWriterLockSlim.EnterUpgradeableReadLock"/> (using <see cref="AcquireUpgradeableReadLock(TimeSpan?, CancellationToken)"/> and <see cref="UpgradeableHandle.UpgradeToWriteLock(TimeSpan?, CancellationToken)"/>)
    /// </summary>
    public sealed class SqlDistributedReaderWriterLock
    {
        private readonly IInternalSqlDistributedLock internalLock;

        #region ---- Constructors ----
        /// <summary>
        /// Creates a lock with name <paramref name="lockName"/>, using the given
        /// <paramref name="connectionString"/> to connect to the database.
        /// 
        /// Uses <see cref="SqlDistributedLockConnectionStrategy.Default"/>
        /// </summary>
        public SqlDistributedReaderWriterLock(string lockName, string connectionString)
            : this(lockName, connectionString, SqlDistributedLockConnectionStrategy.Default)
        {
        }

        /// <summary>
        /// Creates a lock with name <paramref name="lockName"/>, using the given
        /// <paramref name="connectionString"/> to connect to the database via the strategy
        /// specified by <paramref name="connectionStrategy"/>
        /// </summary>
        public SqlDistributedReaderWriterLock(string lockName, string connectionString, SqlDistributedLockConnectionStrategy connectionStrategy)
            : this(lockName, SqlDistributedLock.CreateInternalLock(lockName, connectionString, connectionStrategy))
        {
            if (string.IsNullOrEmpty(connectionString)) { throw new ArgumentNullException(nameof(connectionString)); }
        }

        /// <summary>
        /// Creates a lock with name <paramref name="lockName"/> which, when acquired,
        /// will be scoped to the given <paramref name="connection"/>. The <paramref name="connection"/> is
        /// assumed to be externally managed: the <see cref="SqlDistributedReaderWriterLock"/> will not attempt to open,
        /// close, or dispose it
        /// </summary>
        public SqlDistributedReaderWriterLock(string lockName, IDbConnection connection)
            : this(lockName, new ExternalConnectionOrTransactionSqlDistributedLock(lockName, new ConnectionOrTransaction(connection ?? throw new ArgumentNullException(nameof(connection)))))
        {
        }

        /// <summary>
        /// Creates a lock with name <paramref name="lockName"/> which, when acquired,
        /// will be scoped to the given <paramref name="transaction"/>. The <paramref name="transaction"/> and its
        /// <see cref="IDbTransaction.Connection"/> are assumed to be externally managed: the <see cref="SqlDistributedReaderWriterLock"/> will 
        /// not attempt to open, close, commit, roll back, or dispose them
        /// </summary>
        public SqlDistributedReaderWriterLock(string lockName, IDbTransaction transaction)
            : this(lockName, new ExternalConnectionOrTransactionSqlDistributedLock(lockName, new ConnectionOrTransaction(transaction ?? throw new ArgumentNullException(nameof(transaction)))))
        {
        }

        private SqlDistributedReaderWriterLock(string lockName, IInternalSqlDistributedLock internalLock)
        {
            if (lockName == null) { throw new ArgumentNullException("lockName"); }
            if (lockName.Length > MaxLockNameLength) { throw new FormatException("lockName: must be at most " + MaxLockNameLength + " characters"); }

            this.internalLock = internalLock;
        }
        #endregion

        #region ---- Public API ----
        /// <summary>
        /// Attempts to acquire a READ lock synchronously. Multiple readers are allowed. Not compatible with a WRITE lock
        /// </summary>
        public IDisposable TryAcquireReadLock(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            return cancellationToken.CanBeCanceled
                ? this.TryAcquireWithAsyncCancellation(timeout.ToInt32Timeout(), SqlApplicationLock.SharedLock, cancellationToken)
                : this.internalLock.TryAcquire(timeout.ToInt32Timeout(), SqlApplicationLock.SharedLock, contextHandle: null);
        }

        /// <summary>
        /// Acquires a READ lock synchronously. Multiple readers are allowed. Not compatible with a WRITE lock
        /// </summary>
        public IDisposable AcquireReadLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var handle = this.TryAcquireReadLock(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken);
            DistributedLockHelpers.ValidateTryAcquireResult(handle, timeout);
            return handle;
        }

        /// <summary>
        /// Attempts to acquire a READ lock asynchronously. Multiple readers are allowed. Not compatible with a WRITE lock
        /// </summary>
        public Task<IDisposable> TryAcquireReadLockAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.internalLock.TryAcquireAsync(timeout.ToInt32Timeout(), SqlApplicationLock.SharedLock, cancellationToken, contextHandle: null);
        }

        /// <summary>
        /// Acquires a READ lock asynchronously. Multiple readers are allowed. Not compatible with a WRITE lock
        /// </summary>
        public Task<IDisposable> AcquireReadLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var handleTask = this.TryAcquireReadLockAsync(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken);
            return DistributedLockHelpers.ValidateTryAcquireResultAsync(handleTask, timeout);
        }

        /// <summary>
        /// Attempts to acquire an UPGRADE lock synchronously. Not compatible with another UPGRADE lock or a WRITE lock
        /// </summary>
        public UpgradeableHandle TryAcquireUpgradeableReadLock(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            var handle = cancellationToken.CanBeCanceled
                ? this.TryAcquireWithAsyncCancellation(timeout.ToInt32Timeout(), SqlApplicationLock.UpdateLock, cancellationToken, contextHandle: null)
                : this.internalLock.TryAcquire(timeout.ToInt32Timeout(), SqlApplicationLock.UpdateLock, contextHandle: null);

            return handle != null ? new InternalUpgradeableHandle(this, handle) : null;
        }

        /// <summary>
        /// Acquires an UPGRADE lock synchronously. Not compatible with another UPGRADE lock or a WRITE lock
        /// </summary>
        public UpgradeableHandle AcquireUpgradeableReadLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var handle = this.TryAcquireUpgradeableReadLock(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken);
            DistributedLockHelpers.ValidateTryAcquireResult(handle, timeout);
            return handle;
        }

        /// <summary>
        /// Attempts to acquire an UPGRADE lock asynchronously. Not compatible with another UPGRADE lock or a WRITE lock
        /// </summary>
        public Task<UpgradeableHandle> TryAcquireUpgradeableReadLockAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.TryAcquireUpgradeableReadLockAsync(timeout.ToInt32Timeout(), cancellationToken);
        }

        /// <summary>
        /// Acquires an acquire an UPGRADE lock asynchronously. Not compatible with another UPGRADE lock or a WRITE lock
        /// </summary>
        public Task<UpgradeableHandle> AcquireUpgradeableReadLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var handleTask = this.TryAcquireUpgradeableReadLockAsync(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken);
            return DistributedLockHelpers.ValidateTryAcquireResultAsync(handleTask, timeout);
        }

        /// <summary>
        /// Attempts to acquire a WRITE lock synchronously. Not compatible with another WRITE lock or a UPGRADE lock
        /// </summary>
        public IDisposable TryAcquireWriteLock(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            return cancellationToken.CanBeCanceled
                ? this.TryAcquireWithAsyncCancellation(timeout.ToInt32Timeout(), SqlApplicationLock.ExclusiveLock, cancellationToken, contextHandle: null)
                : this.internalLock.TryAcquire(timeout.ToInt32Timeout(), SqlApplicationLock.ExclusiveLock, contextHandle: null);
        }

        /// <summary>
        /// Acquires a WRITE lock synchronously. Not compatible with another WRITE lock or a UPGRADE lock
        /// </summary>
        public IDisposable AcquireWriteLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var handle = this.TryAcquireWriteLock(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken);
            DistributedLockHelpers.ValidateTryAcquireResult(handle, timeout);
            return handle;
        }

        /// <summary>
        /// Attempts to acquire a WRITE lock asynchronously. Not compatible with another WRITE lock or a UPGRADE lock
        /// </summary>
        public Task<IDisposable> TryAcquireWriteLockAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.internalLock.TryAcquireAsync(timeout.ToInt32Timeout(), SqlApplicationLock.ExclusiveLock, cancellationToken, contextHandle: null);
        }

        /// <summary>
        /// Acquires a WRITE lock asynchronously. Not compatible with another WRITE lock or a UPGRADE lock
        /// </summary>
        public Task<IDisposable> AcquireWriteLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var handleTask = this.TryAcquireWriteLockAsync(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken);
            return DistributedLockHelpers.ValidateTryAcquireResultAsync(handleTask, timeout);
        }

        /// <summary>
        /// The maximum allowed length for lock names. See https://msdn.microsoft.com/en-us/library/ms189823.aspx
        /// </summary>
        public static int MaxLockNameLength => SqlDistributedLock.MaxLockNameLength;

        /// <summary>
        /// Given <paramref name="baseLockName"/>, constructs a lock name which is safe for use with <see cref="SqlDistributedReaderWriterLock"/>
        /// </summary>
        public static string GetSafeLockName(string baseLockName) => SqlDistributedLock.GetSafeLockName(baseLockName);

        /// <summary>
        /// A lock handle which can be upgraded to an exclusive WRITE lock
        /// </summary>
        public abstract class UpgradeableHandle : IDisposable
        {
            // forbid external inheritors
            internal UpgradeableHandle() { }

            /// <summary>
            /// Attempts to upgrade a WRITE lock synchronously. Not compatible with another WRITE lock or a UPGRADE lock
            /// </summary>
            public abstract bool TryUpgradeToWriteLock(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken));

            /// <summary>
            /// Upgrades to a WRITE lock synchronously. Not compatible with another WRITE lock or a UPGRADE lock
            /// </summary>
            public bool UpgradeToWriteLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
            {
                var succeeded = this.TryUpgradeToWriteLock(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken);
                DistributedLockHelpers.ValidateTryAcquireResult(succeeded, timeout);
                return succeeded;
            }

            /// <summary>
            /// Attempts to upgrade a WRITE lock asynchronously. Not compatible with another WRITE lock or a UPGRADE lock
            /// </summary>
            public abstract Task<bool> TryUpgradeToWriteLockAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken));

            /// <summary>
            /// Upgrades to a WRITE lock asynchronously. Not compatible with another WRITE lock or a UPGRADE lock
            /// </summary>
            public Task<bool> UpgradeToWriteLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
            {
                var upgradeTask = this.TryUpgradeToWriteLockAsync(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken);
                return this.ValidateUpgradeAsync(upgradeTask, timeout);
            }

            private async Task<bool> ValidateUpgradeAsync(Task<bool> upgradeTask, TimeSpan? timeout)
            {
                var succeeded = await upgradeTask.ConfigureAwait(false);
                DistributedLockHelpers.ValidateTryAcquireResult(succeeded, timeout);
                return succeeded;
            }

            /// <summary>
            /// Releases the lock
            /// </summary>
            public abstract void Dispose();
        }
        #endregion

        private IDisposable TryAcquireWithAsyncCancellation(int timeoutMillis, ISqlSynchronizationStrategy<object> strategy, CancellationToken cancellationToken, IDisposable contextHandle = null)
        {
            var tryAcquireTask = this.internalLock.TryAcquireAsync(timeoutMillis, strategy, cancellationToken, contextHandle);
            try
            {
                return tryAcquireTask.Result;
            }
            catch (AggregateException ex)
            {
                // attempt to prevent the throwing of aggregate exceptions
                if (ex.InnerExceptions.Count == 1)
                {
                    // rethrow the inner exception without losing stack trace
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }
                throw; // otherwise just rethrow
            }
        }

        private async Task<UpgradeableHandle> TryAcquireUpgradeableReadLockAsync(int timeoutMillis, CancellationToken cancellationToken)
        {
            var handle = await this.internalLock.TryAcquireAsync(timeoutMillis, SqlApplicationLock.UpdateLock, cancellationToken, contextHandle: null).ConfigureAwait(false);
            return handle != null ? new InternalUpgradeableHandle(this, handle) : null;
        }

        private sealed class InternalUpgradeableHandle : UpgradeableHandle
        {
            private SqlDistributedReaderWriterLock @lock;
            private IDisposable baseHandle, upgradedHandle;

            public InternalUpgradeableHandle(SqlDistributedReaderWriterLock @lock, IDisposable baseHandle)
            {
                this.@lock = @lock;
                this.baseHandle = baseHandle;
            }

            public override bool TryUpgradeToWriteLock(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
            {
                this.CheckHandles();

                this.upgradedHandle = cancellationToken.CanBeCanceled
                    ? this.@lock.TryAcquireWithAsyncCancellation(timeout.ToInt32Timeout(), SqlApplicationLock.ExclusiveLock, cancellationToken, contextHandle: this.baseHandle)
                    : this.@lock.internalLock.TryAcquire(timeout.ToInt32Timeout(), SqlApplicationLock.ExclusiveLock, contextHandle: this.baseHandle);
                return this.upgradedHandle != null;
            }

            public override Task<bool> TryUpgradeToWriteLockAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
            {
                this.CheckHandles();

                return this.TryUpgradeToWriteLockAsync(timeout.ToInt32Timeout(), cancellationToken);
            }

            private async Task<bool> TryUpgradeToWriteLockAsync(int timeoutMillis, CancellationToken cancellationToken)
            {
                this.upgradedHandle = await this.@lock.internalLock.TryAcquireAsync(timeoutMillis, SqlApplicationLock.ExclusiveLock, cancellationToken, contextHandle: this.baseHandle).ConfigureAwait(false);
                return this.upgradedHandle != null;
            }

            private void CheckHandles()
            {
                if (this.baseHandle == null) { throw new ObjectDisposedException(nameof(UpgradeableHandle)); }
                if (this.upgradedHandle != null) { throw new InvalidOperationException("the lock has already been upgraded"); }
            }

            public override void Dispose()
            {
                // NOTE that despite the use of Interlocked here, this class is not thread-safe
                var innerHandle = Interlocked.Exchange(ref this.baseHandle, null);
                if (innerHandle != null)
                {
                    // sp_getapplock must be exited for each time it is entered
                    this.upgradedHandle?.Dispose();
                    innerHandle.Dispose();
                    this.@lock = null;
                }
            }
        }
    }
}
