using System.Data;

namespace Medallion.Threading.GBase;

/// <summary>
/// Implements <see cref="IDistributedLockProvider"/> for <see cref="GBaseDistributedLock"/>
/// and <see cref="IDistributedUpgradeableReaderWriterLockProvider"/> for <see cref="GBaseDistributedReaderWriterLock"/>
/// </summary>
public sealed class GBaseDistributedSynchronizationProvider : IDistributedLockProvider, IDistributedUpgradeableReaderWriterLockProvider
{
    private readonly Func<string, bool, GBaseDistributedLock> _lockFactory;
    private readonly Func<string, bool, GBaseDistributedReaderWriterLock> _readerWriterLockFactory;

    /// <summary>
    /// Constructs a provider that connects with <paramref name="connectionString"/> and <paramref name="options"/>.
    /// </summary>
    public GBaseDistributedSynchronizationProvider(string connectionString, Action<GBaseConnectionOptionsBuilder>? options = null)
    {
        if (connectionString == null) { throw new ArgumentNullException(nameof(connectionString)); }

        this._lockFactory = (name, exactName) => new GBaseDistributedLock(name, connectionString, options, exactName);
        this._readerWriterLockFactory = (name, exactName) => new GBaseDistributedReaderWriterLock(name, connectionString, options, exactName);
    }

    /// <summary>
    /// Constructs a provider that connects with <paramref name="connection"/>.
    /// </summary>
    public GBaseDistributedSynchronizationProvider(IDbConnection connection)
    {
        if (connection == null) { throw new ArgumentNullException(nameof(connection)); }

        this._lockFactory = (name, exactName) => new GBaseDistributedLock(name, connection, exactName);
        this._readerWriterLockFactory = (name, exactName) => new GBaseDistributedReaderWriterLock(name, connection, exactName);
    }

    /// <summary>
    /// Creates a <see cref="GBaseDistributedLock"/> with the provided <paramref name="name"/>. Unless <paramref name="exactName"/> 
    /// is specified, invalid names will be escaped/hashed.
    /// </summary>
    public GBaseDistributedLock CreateLock(string name, bool exactName = false) => this._lockFactory(name, exactName);

    IDistributedLock IDistributedLockProvider.CreateLock(string name) => this.CreateLock(name);

    /// <summary>
    /// Creates a <see cref="GBaseDistributedReaderWriterLock"/> with the provided <paramref name="name"/>. Unless <paramref name="exactName"/> 
    /// is specified, invalid names will be escaped/hashed.
    /// </summary>
    public GBaseDistributedReaderWriterLock CreateReaderWriterLock(string name, bool exactName = false) => this._readerWriterLockFactory(name, exactName);

    IDistributedUpgradeableReaderWriterLock IDistributedUpgradeableReaderWriterLockProvider.CreateUpgradeableReaderWriterLock(string name) =>
        this.CreateReaderWriterLock(name);

    IDistributedReaderWriterLock IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string name) =>
        this.CreateReaderWriterLock(name);
}
