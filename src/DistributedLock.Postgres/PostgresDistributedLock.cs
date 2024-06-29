using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using System.Data;
#if NET7_0_OR_GREATER
using System.Data.Common;
#endif

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

#if NET7_0_OR_GREATER
    /// <summary>
    /// Constructs a lock with the given <paramref name="key"/> (effectively the lock name) and <paramref name="dbDataSource"/>,
    /// and <paramref name="options"/>
    /// </summary>
    public PostgresDistributedLock(PostgresAdvisoryLockKey key, DbDataSource dbDataSource, Action<PostgresConnectionOptionsBuilder>? options = null)
        : this(key, CreateInternalLock(key, dbDataSource, options))
    {
    }
#endif

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

        var (keepaliveCadence, useTransaction, useMultiplexing) = PostgresConnectionOptionsBuilder.GetOptions(options);

        return useMultiplexing
            ? new OptimisticConnectionMultiplexingDbDistributedLock(key.ToString(), connectionString, PostgresMultiplexedConnectionLockPool.Instance, keepaliveCadence)
            : new DedicatedConnectionOrTransactionDbDistributedLock(key.ToString(), () => new PostgresDatabaseConnection(connectionString), useTransaction: useTransaction, keepaliveCadence);
    }

    internal static IDbDistributedLock CreateInternalLock(PostgresAdvisoryLockKey key, IDbConnection connection)
    {
        if (connection == null) { throw new ArgumentNullException(nameof(connection)); }
        return new DedicatedConnectionOrTransactionDbDistributedLock(key.ToString(), () => new PostgresDatabaseConnection(connection));
    }

#if NET7_0_OR_GREATER
    internal static IDbDistributedLock CreateInternalLock(PostgresAdvisoryLockKey key, DbDataSource dbDataSource, Action<PostgresConnectionOptionsBuilder>? options)
    {
        if (dbDataSource == null) { throw new ArgumentNullException(nameof(dbDataSource)); }

        // Multiplexing is currently incompatible with DbDataSource, so default it to false
        var originalOptions = options;
        options = o => { o.UseMultiplexing(false); originalOptions?.Invoke(o); };

        var (keepaliveCadence, useTransaction, useMultiplexing) = PostgresConnectionOptionsBuilder.GetOptions(options);

        return useMultiplexing
            ? throw new NotSupportedException("Multiplexing is current incompatible with DbDataSource.")
            : new DedicatedConnectionOrTransactionDbDistributedLock(key.ToString(), () => new PostgresDatabaseConnection(dbDataSource), useTransaction: useTransaction, keepaliveCadence);
    }
#endif
}