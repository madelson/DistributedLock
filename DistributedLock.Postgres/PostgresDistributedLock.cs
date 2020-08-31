using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Postgres
{
    // todo integrate into all appropriate abstract test cases (will want a new provider concept to abstract away pool clearing, credentials, DbProviderFactory, etc)

    /// <summary>
    /// Implements a distributed lock using Postgres advisory locks
    /// (see https://www.postgresql.org/docs/12/functions-admin.html#FUNCTIONS-ADVISORY-LOCKS)
    /// </summary>
    public sealed partial class PostgresDistributedLock : IInternalDistributedLock<PostgresDistributedLockHandle>
    {
        private readonly IDbDistributedLock _internalLock;

        // todo revisit API
        /// <summary>
        /// Constructs a lock with the given <paramref name="key"/> (effectively the lock name), <paramref name="connectionString"/>,
        /// and <paramref name="options"/>
        /// </summary>
        public PostgresDistributedLock(PostgresAdvisoryLockKey key, string connectionString, Action<PostgresConnectionOptionsBuilder>? options = null)
            : this(key, CreateInternalLock(key, connectionString, options))
        {
        }

        /// <summary>
        /// Constructs a lock with the given <paramref name="key"/> (effectively the lock name) and <paramref name="connection"/>.
        /// </summary>
        public PostgresDistributedLock(PostgresAdvisoryLockKey key, IDbConnection connection)
            : this(key, CreateInternalLock(key, connection))
        {
        }

        private PostgresDistributedLock(PostgresAdvisoryLockKey key, IDbDistributedLock internalLock)
        {
            this.Key = key;
            this._internalLock = internalLock;
        }

        // todo consider API with name
        /// <summary>
        /// The lock name
        /// </summary>
        public PostgresAdvisoryLockKey Key { get; }

        string IDistributedLock.Name => this.Key.ToString();

        bool IDistributedLock.IsReentrant => false;

        /// <summary>
        /// Equivalent to <see cref="IDistributedLockProvider.GetSafeLockName(string)"/>
        /// </summary>
        public static PostgresAdvisoryLockKey GetSafeName(string name) => new PostgresAdvisoryLockKey(name, allowHashing: true);

        ValueTask<PostgresDistributedLockHandle?> IInternalDistributedLock<PostgresDistributedLockHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken) =>
            this._internalLock.TryAcquireAsync(timeout, PostgresAdvisoryLock.ExclusiveLock, cancellationToken, contextHandle: null).Wrap(h => new PostgresDistributedLockHandle(h));

        internal static IDbDistributedLock CreateInternalLock(PostgresAdvisoryLockKey key, string connectionString, Action<PostgresConnectionOptionsBuilder>? options)
        {
            if (connectionString == null) { throw new ArgumentNullException(nameof(connectionString)); }

            var (keepaliveCadence, useMultiplexing) = PostgresConnectionOptionsBuilder.GetOptions(options);

            if (useMultiplexing)
            {
                return new OptimisticConnectionMultiplexingDbDistributedLock(key.ToString(), connectionString, PostgresMultiplexedConnectionLockPool.Instance, keepaliveCadence);
            }

            return new DedicatedConnectionOrTransactionDbDistributedLock(key.ToString(), () => new PostgresDatabaseConnection(connectionString), useTransaction: false, keepaliveCadence);
        }

        internal static IDbDistributedLock CreateInternalLock(PostgresAdvisoryLockKey key, IDbConnection connection)
        {
            if (connection == null) { throw new ArgumentNullException(nameof(connection)); }
            return new DedicatedConnectionOrTransactionDbDistributedLock(key.ToString(), () => new PostgresDatabaseConnection(connection));
        }
    }
}
