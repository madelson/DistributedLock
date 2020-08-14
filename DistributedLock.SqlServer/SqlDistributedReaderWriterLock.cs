using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.SqlServer
{
    // todo rename assembly to .SqlServer

    /// <summary>
    /// Implements reader-writer lock semantics using a SQL server application lock
    /// (see https://msdn.microsoft.com/en-us/library/ms189823.aspx).
    /// 
    /// This class supports the following patterns:
    /// * Multiple readers AND single writer (using <see cref="AcquireReadLock(TimeSpan?, CancellationToken)"/> and <see cref="AcquireUpgradeableReadLock(TimeSpan?, CancellationToken)"/>)
    /// * Multiple readers OR single writer (using <see cref="AcquireReadLock(TimeSpan?, CancellationToken)"/> and <see cref="AcquireWriteLock(TimeSpan?, CancellationToken)"/>)
    /// * Upgradeable read locks similar to <see cref="ReaderWriterLockSlim.EnterUpgradeableReadLock"/> (using <see cref="AcquireUpgradeableReadLock(TimeSpan?, CancellationToken)"/> and <see cref="IDistributedLockUpgradeableHandle.UpgradeToWriteLock(TimeSpan?, CancellationToken)"/>)
    /// </summary>
    public sealed partial class SqlDistributedReaderWriterLock : IInternalDistributedUpgradeableReaderWriterLock<SqlDistributedReaderWriterLockHandle, SqlDistributedReaderWriterLockUpgradeableHandle>
    {
        private readonly IDbDistributedLock _internalLock;

        #region ---- Constructors ----
        /// <summary>
        /// Constructs a new lock using the provided <paramref name="name"/>. 
        /// 
        /// The provided <paramref name="connectionString"/> will be used to connect to the database.
        /// 
        /// Unless <paramref name="exactName"/> is specified, <see cref="GetSafeName(string)"/> will be used to ensure name validity.
        /// </summary>
        public SqlDistributedReaderWriterLock(string name, string connectionString, Action<SqlConnectionOptionsBuilder>? options = null, bool exactName = false)
            : this(name, exactName, n => SqlDistributedLock.CreateInternalLock(n, connectionString, options))
        {
        }

        /// <summary>
        /// Constructs a new lock using the provided <paramref name="name"/>.
        /// 
        /// The provided <paramref name="connection"/> will be used to connect to the database and will provide lock scope. It is assumed to be externally managed and
        /// will not be opened or closed.
        /// 
        /// Unless <paramref name="exactName"/> is specified, <see cref="GetSafeName(string)"/> will be used to ensure name validity.
        /// </summary>
        public SqlDistributedReaderWriterLock(string name, IDbConnection connection, bool exactName = false)
            : this(name, exactName, n => SqlDistributedLock.CreateInternalLock(n, connection))
        {
        }

        /// <summary>
        /// Constructs a new lock using the provided <paramref name="name"/>.
        /// 
        /// The provided <paramref name="transaction"/> will be used to connect to the database and will provide lock scope. It is assumed to be externally managed and
        /// will not be committed or rolled back.
        /// 
        /// Unless <paramref name="exactName"/> is specified, <see cref="GetSafeName(string)"/> will be used to ensure name validity.
        /// </summary>
        public SqlDistributedReaderWriterLock(string name, IDbTransaction transaction, bool exactName = false)
            : this(name, exactName, n => SqlDistributedLock.CreateInternalLock(n, transaction))
        {
        }

        private SqlDistributedReaderWriterLock(string name, bool exactName, Func<string, IDbDistributedLock> internalLockFactory)
        {
            if (exactName)
            {
                if (name == null) { throw new ArgumentNullException(nameof(name)); }
                if (name.Length > MaxNameLength) { throw new FormatException($"{nameof(name)}: must be at most {MaxNameLength} characters"); }
                this.Name = name;
            }
            else
            {
                this.Name = GetSafeName(name);
            }

            this._internalLock = internalLockFactory(this.Name);
        }
        #endregion

        /// <summary>
        /// Implements <see cref="IDistributedLock.Name"/>
        /// </summary>
        public string Name { get; }

        bool IDistributedReaderWriterLock.IsReentrant => throw new NotImplementedException();

        /// <summary>
        /// The maximum allowed length for lock names. See https://msdn.microsoft.com/en-us/library/ms189823.aspx
        /// </summary>
        public static int MaxNameLength => SqlDistributedLock.MaxNameLength;

        /// <summary>
        /// Given <paramref name="name"/>, constructs a lock name which is safe for use with <see cref="SqlDistributedReaderWriterLock"/>
        /// </summary>
        public static string GetSafeName(string name) => SqlDistributedLock.GetSafeName(name);

        async ValueTask<SqlDistributedReaderWriterLockUpgradeableHandle?> IInternalDistributedUpgradeableReaderWriterLock<SqlDistributedReaderWriterLockHandle, SqlDistributedReaderWriterLockUpgradeableHandle>.InternalTryAcquireUpgradeableReadLockAsync(
            TimeoutValue timeout, 
            CancellationToken cancellationToken)
        {
            var innerHandle = await this._internalLock
                .TryAcquireAsync(timeout, SqlApplicationLock.UpdateLock, cancellationToken, contextHandle: null).ConfigureAwait(false);
            return innerHandle != null ? new SqlDistributedReaderWriterLockUpgradeableHandle(innerHandle, this._internalLock) : null;
        }

        async ValueTask<SqlDistributedReaderWriterLockHandle?> IInternalDistributedReaderWriterLock<SqlDistributedReaderWriterLockHandle>.InternalTryAcquireAsync(
            TimeoutValue timeout, 
            CancellationToken cancellationToken, 
            bool isWrite)
        {
            var innerHandle = await this._internalLock
                .TryAcquireAsync(timeout, isWrite ? SqlApplicationLock.ExclusiveLock : SqlApplicationLock.SharedLock, cancellationToken, contextHandle: null).ConfigureAwait(false);
            return innerHandle != null ? new SqlDistributedReaderWriterLockNonUpgradeableHandle(innerHandle) : null;
        }
    }
}
