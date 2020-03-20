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
        public PostgresDistributedLock(PostgresAdvisoryLockKey key, string connectionString, bool useMultiplexing = true)
            : this(
                key,
                useMultiplexing
                    ? new OptimisticConnectionMultiplexingDbDistributedLock(
                        key.ToString(),
                        connectionString ?? throw new ArgumentNullException(nameof(connectionString)),
                        PostgresMultiplexedConnectionLockPool.Instance
                    )
                    : CreateOwnedConnectionLock(key, connectionString))
        {
        }

        public PostgresDistributedLock(PostgresAdvisoryLockKey key, IDbConnection connection)
            : this(
                key, 
                new ExternalConnectionOrTransactionDbDistributedLock(
                    key.ToString(), 
                    new PostgresDatabaseConnection(connection ?? throw new ArgumentNullException(nameof(connection)), Timeout.InfiniteTimeSpan)
                ))
        {
        }

        private PostgresDistributedLock(PostgresAdvisoryLockKey key, IDbDistributedLock internalLock)
        {
            this.Key = key;
            this._internalLock = internalLock;
        }

        // todo consider API with name
        public PostgresAdvisoryLockKey Key { get; }

        string IDistributedLock.Name => this.Key.ToString();

        bool IDistributedLock.IsReentrant => false;

        public static PostgresAdvisoryLockKey GetSafeName(string name) => new PostgresAdvisoryLockKey(name, allowHashing: true);

        ValueTask<PostgresDistributedLockHandle?> IInternalDistributedLock<PostgresDistributedLockHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken) =>
            this._internalLock.TryAcquireAsync(timeout, PostgresAdvisoryLock.ExclusiveLock, cancellationToken, contextHandle: null).Wrap(h => new PostgresDistributedLockHandle(h));

        // todo remove
        public bool WillGoAsync(TimeoutValue timeout, CancellationToken cancellationToken) => false;

        private static IDbDistributedLock CreateOwnedConnectionLock(PostgresAdvisoryLockKey key, string connectionString)
        {
            if (connectionString == null) { throw new ArgumentNullException(nameof(connectionString)); }

            return new OwnedConnectionOrTransactionDbDistributedLock(
                key.ToString(),
                () => new PostgresDatabaseConnection(connectionString, Timeout.InfiniteTimeSpan),
                useTransaction: false
            );
        }
    }
}
