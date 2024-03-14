using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using System.Data;

namespace Medallion.Threading.Postgres;

/// <summary>
/// Implements a distributed lock using Postgres advisory locks
/// (see https://www.postgresql.org/docs/12/functions-admin.html#FUNCTIONS-ADVISORY-LOCKS)
/// </summary>
public sealed partial class PostgresDistributedLock : IInternalDistributedLock<PostgresDistributedLockHandle>
{
    private readonly IDbDistributedLock _internalLock;

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

    /// <summary>
    /// The <see cref="PostgresAdvisoryLockKey"/> that uniquely identifies the lock on the database
    /// </summary>
    public PostgresAdvisoryLockKey Key { get; }

    string IDistributedLock.Name => this.Key.ToString();

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
