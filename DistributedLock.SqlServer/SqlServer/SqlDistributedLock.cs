using Medallion.Threading.Internal;
using Medallion.Threading.Sql.ConnectionMultiplexing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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
    public sealed partial class SqlDistributedLock : IInternalDistributedLock<SqlDistributedLockHandle>
    {
        private readonly IInternalSqlDistributedLock _internalLock;

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
            : this(name, exactName, n => new ExternalConnectionOrTransactionSqlDistributedLock(n, new ConnectionOrTransaction(connection ?? throw new ArgumentNullException(nameof(connection)))))
        {
        }

        public SqlDistributedLock(string name, IDbTransaction transaction, bool exactName = false)
            : this(name, exactName, n => new ExternalConnectionOrTransactionSqlDistributedLock(n, new ConnectionOrTransaction(transaction ?? throw new ArgumentNullException(nameof(transaction)))))
        {
        }

        private SqlDistributedLock(string name, bool exactName, Func<string, IInternalSqlDistributedLock> internalLockFactory)
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

        public bool IsReentrant => this._internalLock.IsReentrant;

        /// <summary>
        /// Given <paramref name="name"/>, constructs a lock name which is safe for use with <see cref="SqlDistributedLock"/>
        /// </summary>
        public static string GetSafeName(string name) =>
            DistributedLockHelpers.ToSafeLockName(name, MaxNameLength, s => s);

        bool IInternalDistributedLock<SqlDistributedLockHandle>.WillGoAsync(TimeoutValue timeout, CancellationToken cancellationToken) => false;

        ValueTask<SqlDistributedLockHandle?> IInternalDistributedLock<SqlDistributedLockHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken) =>
            HandleHelpers.Wrap(this._internalLock.TryAcquireAsync(timeout, SqlApplicationLock.ExclusiveLock, cancellationToken, contextHandle: null), h => new SqlDistributedLockHandle(h));

        internal static IInternalSqlDistributedLock CreateInternalLock(string name, string connectionString, SqlDistributedLockConnectionStrategy connectionStrategy)
        {
            if (connectionString == null) { throw new ArgumentNullException(nameof(connectionString)); }

            switch (connectionStrategy) 
            {
                case SqlDistributedLockConnectionStrategy.Default:
                case SqlDistributedLockConnectionStrategy.Connection:
                    return new OwnedConnectionSqlDistributedLock(name: name, connectionString: connectionString);
                case SqlDistributedLockConnectionStrategy.Transaction:
                    return new OwnedTransactionSqlDistributedLock(name: name, connectionString: connectionString);
                case SqlDistributedLockConnectionStrategy.OptimisticConnectionMultiplexing:
                    return new OptimisticConnectionMultiplexingSqlDistributedLock(name: name, connectionString: connectionString);
                case SqlDistributedLockConnectionStrategy.Azure:
                    return new AzureSqlDistributedLock(lockName: name, connectionString: connectionString);
                default:
                    throw new ArgumentException(nameof(connectionStrategy));
            }
        }
    }
}
