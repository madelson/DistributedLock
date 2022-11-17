using System.Data;

namespace Medallion.Threading.Postgres;

/// <summary>
/// Implements <see cref="IDistributedLockProvider"/> for <see cref="PostgresDistributedLock"/> and
/// <see cref="IDistributedReaderWriterLockProvider"/> for <see cref="PostgresDistributedReaderWriterLock"/>.
/// </summary>
public sealed class PostgresDistributedSynchronizationProvider : IDistributedLockProvider, IDistributedReaderWriterLockProvider
{
    private readonly Func<PostgresAdvisoryLockKey, PostgresDistributedLock> _lockFactory;
    private readonly Func<PostgresAdvisoryLockKey, PostgresDistributedReaderWriterLock> _readerWriterLockFactory;

    /// <summary>
    /// Constructs a provider which connects to Postgres using the provided <paramref name="connectionString"/> and <paramref name="options"/>.
    /// </summary>
    public PostgresDistributedSynchronizationProvider(string connectionString, Action<PostgresConnectionOptionsBuilder>? options = null)
    {
        if (connectionString == null) { throw new ArgumentNullException(nameof(connectionString)); }

        this._lockFactory = key => new PostgresDistributedLock(key, connectionString, options);
        this._readerWriterLockFactory = key => new PostgresDistributedReaderWriterLock(key, connectionString, options);
    }

    /// <summary>
    /// Constructs a provider which connects to Postgres using the provided <paramref name="connection"/>.
    /// </summary>
    public PostgresDistributedSynchronizationProvider(IDbConnection connection)
    {
        if (connection == null) { throw new ArgumentNullException(nameof(connection)); }

        this._lockFactory = key => new PostgresDistributedLock(key, connection);
        this._readerWriterLockFactory = key => new PostgresDistributedReaderWriterLock(key, connection);
    }

    /// <summary>
    /// Creates a <see cref="PostgresDistributedLock"/> with the provided <paramref name="key"/>.
    /// </summary>
    public PostgresDistributedLock CreateLock(PostgresAdvisoryLockKey key) => this._lockFactory(key);

    IDistributedLock IDistributedLockProvider.CreateLock(string name) => 
        this.CreateLock(new PostgresAdvisoryLockKey(name, allowHashing: true));

    /// <summary>
    /// Creates a <see cref="PostgresDistributedReaderWriterLock"/> with the provided <paramref name="key"/>.
    /// </summary>
    public PostgresDistributedReaderWriterLock CreateReaderWriterLock(PostgresAdvisoryLockKey key) => this._readerWriterLockFactory(key);

    IDistributedReaderWriterLock IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string name) =>
        this.CreateReaderWriterLock(new PostgresAdvisoryLockKey(name, allowHashing: true));
}
