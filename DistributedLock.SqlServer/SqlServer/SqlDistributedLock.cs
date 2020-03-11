using Medallion.Threading.Data;
using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.SqlServer
{
    /// <summary>
    /// Implements a distributed lock using a SQL server application lock
    /// (see https://msdn.microsoft.com/en-us/library/ms189823.aspx)
    /// </summary>
    public sealed partial class SqlDistributedLock : IInternalDistributedLock<SqlDistributedLockHandle>
    {
        private readonly IDbDistributedLock _internalLock;

        // todo review API (here + rwlock + semaphore); should we have options instead of an enum?
        // todo connection factory API (to allow for access tokens)?

        public SqlDistributedLock(string name, string connectionString, bool exactName = false)
            : this(name, exactName, n => CreateInternalLock(n, connectionString, SqlDistributedLockConnectionStrategy.Default))
        {
        }

        public SqlDistributedLock(string name, string connectionString, SqlDistributedLockConnectionStrategy connectionStrategy, bool exactName = false)
            : this(name, exactName, n => CreateInternalLock(n, connectionString, connectionStrategy))
        {
        }

        public SqlDistributedLock(string name, IDbConnection connection, bool exactName = false)
            : this(name, exactName, n => new ExternalConnectionOrTransactionDbDistributedLock(n, new SqlDatabaseConnection(connection ?? throw new ArgumentNullException(nameof(connection)), Timeout.InfiniteTimeSpan)))
        {
        }

        public SqlDistributedLock(string name, IDbTransaction transaction, bool exactName = false)
            : this(name, exactName, n => new ExternalConnectionOrTransactionDbDistributedLock(n, new SqlDatabaseConnection(transaction ?? throw new ArgumentNullException(nameof(transaction)), Timeout.InfiniteTimeSpan)))
        {
        }

        private SqlDistributedLock(string name, bool exactName, Func<string, IDbDistributedLock> internalLockFactory)
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

        /// <summary>
        /// The maximum allowed length for lock names. See https://msdn.microsoft.com/en-us/library/ms189823.aspx
        /// </summary>
        public static int MaxNameLength => 255;

        // todo should this be the safe name or the user-provided name? Should we even expose this?
        public string Name { get; }

        public bool IsReentrant => throw new NotImplementedException("todo");

        /// <summary>
        /// Given <paramref name="name"/>, constructs a lock name which is safe for use with <see cref="SqlDistributedLock"/>
        /// </summary>
        public static string GetSafeName(string name) =>
            DistributedLockHelpers.ToSafeLockName(name, MaxNameLength, s => s);

        bool IInternalDistributedLock<SqlDistributedLockHandle>.WillGoAsync(TimeoutValue timeout, CancellationToken cancellationToken) => false;

        ValueTask<SqlDistributedLockHandle?> IInternalDistributedLock<SqlDistributedLockHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken) =>
            HandleHelpers.Wrap(this._internalLock.TryAcquireAsync(timeout, SqlApplicationLock.ExclusiveLock, cancellationToken, contextHandle: null), h => new SqlDistributedLockHandle(h));

        internal static IDbDistributedLock CreateInternalLock(string name, string connectionString, SqlDistributedLockConnectionStrategy connectionStrategy)
        {
            if (connectionString == null) { throw new ArgumentNullException(nameof(connectionString)); }

            switch (connectionStrategy) 
            {
                // todo replace enum with connect options
                case SqlDistributedLockConnectionStrategy.Default:
                case SqlDistributedLockConnectionStrategy.Connection:
                    return new OwnedConnectionOrTransactionDbDistributedLock(name, ConnectionFactory, useTransaction: false);
                case SqlDistributedLockConnectionStrategy.Transaction:
                    return new OwnedConnectionOrTransactionDbDistributedLock(name, ConnectionFactory, useTransaction: true);
                case SqlDistributedLockConnectionStrategy.OptimisticConnectionMultiplexing:
                    return new OptimisticConnectionMultiplexingDbDistributedLock(name: name, connectionString, SqlMultiplexedConnectionLockPool.Instance);
                case SqlDistributedLockConnectionStrategy.Azure:
                    // todo this should not be its own separate case
                    return new OwnedConnectionOrTransactionDbDistributedLock(name, () => new SqlDatabaseConnection(connectionString, KeepaliveHelper.Interval), useTransaction: true);
                default:
                    throw new ArgumentException(nameof(connectionStrategy));
            }

            DatabaseConnection ConnectionFactory() => new SqlDatabaseConnection(connectionString, Timeout.InfiniteTimeSpan);
        }
    }
}
