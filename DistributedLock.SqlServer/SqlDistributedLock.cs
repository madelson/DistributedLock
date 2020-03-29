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

        // todo connection factory API (to allow for access tokens)?

        public SqlDistributedLock(string name, string connectionString, Action<SqlConnectionOptionsBuilder>? options = null, bool exactName = false)
            : this(name, exactName, n => CreateInternalLock(n, connectionString, options))
        {
        }

        public SqlDistributedLock(string name, IDbConnection connection, bool exactName = false)
            : this(name, exactName, n => new ExternalConnectionOrTransactionDbDistributedLock(n, new SqlDatabaseConnection(connection ?? throw new ArgumentNullException(nameof(connection)))))
        {
        }

        public SqlDistributedLock(string name, IDbTransaction transaction, bool exactName = false)
            : this(name, exactName, n => new ExternalConnectionOrTransactionDbDistributedLock(n, new SqlDatabaseConnection(transaction ?? throw new ArgumentNullException(nameof(transaction)))))
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
            DistributedLockHelpers.ToSafeName(name, MaxNameLength, s => s);

        bool IInternalDistributedLock<SqlDistributedLockHandle>.WillGoAsync(TimeoutValue timeout, CancellationToken cancellationToken) => false;

        ValueTask<SqlDistributedLockHandle?> IInternalDistributedLock<SqlDistributedLockHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken) =>
            this._internalLock.TryAcquireAsync(timeout, SqlApplicationLock.ExclusiveLock, cancellationToken, contextHandle: null).Wrap(h => new SqlDistributedLockHandle(h));

        internal static IDbDistributedLock CreateInternalLock(string name, string connectionString, Action<SqlConnectionOptionsBuilder>? optionsBuilder)
        {
            if (connectionString == null) { throw new ArgumentNullException(nameof(connectionString)); }

            var (keepaliveCadence, useTransaction, useMultiplexing) = SqlConnectionOptionsBuilder.GetOptions(optionsBuilder);

            if (useMultiplexing)
            {
                return new OptimisticConnectionMultiplexingDbDistributedLock(name, connectionString, SqlMultiplexedConnectionLockPool.Instance, keepaliveCadence);
            }

            return new OwnedConnectionOrTransactionDbDistributedLock(name, () => new SqlDatabaseConnection(connectionString), useTransaction: useTransaction, keepaliveCadence);
        }
    }
}
