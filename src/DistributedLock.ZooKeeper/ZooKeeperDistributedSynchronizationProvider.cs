namespace Medallion.Threading.ZooKeeper;

/// <summary>
/// Implements <see cref="IDistributedLockProvider"/> for <see cref="ZooKeeperDistributedLock"/>,
/// <see cref="IDistributedReaderWriterLockProvider"/> for <see cref="ZooKeeperDistributedReaderWriterLock"/>,
/// and <see cref="IDistributedSemaphoreProvider"/> for <see cref="ZooKeeperDistributedSemaphore"/>.
/// </summary>
public sealed class ZooKeeperDistributedSynchronizationProvider : IDistributedLockProvider, IDistributedReaderWriterLockProvider, IDistributedSemaphoreProvider
{
    private readonly ZooKeeperPath _directoryPath;
    private readonly string _connectionString;
    private readonly Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? _options;

    /// <summary>
    /// Constructs a provider which uses <paramref name="connectionString"/> and <paramref name="options"/>. Lock and semaphore nodes will be created
    /// in the root directory '/'.
    /// </summary>
    public ZooKeeperDistributedSynchronizationProvider(string connectionString, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? options = null)
        : this(ZooKeeperPath.Root, connectionString, options) { }

    /// <summary>
    /// Constructs a provider which uses <paramref name="connectionString"/> and <paramref name="options"/>. Lock and semaphore nodes will be created
    /// in <paramref name="directoryPath"/>.
    /// </summary>
    public ZooKeeperDistributedSynchronizationProvider(ZooKeeperPath directoryPath, string connectionString, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? options = null)
    {
        this._directoryPath = directoryPath != default ? directoryPath : throw new ArgumentNullException(nameof(directoryPath));
        this._connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        this._options = options;
    }

    /// <summary>
    /// Creates a <see cref="ZooKeeperDistributedLock"/> using the given <paramref name="name"/>.
    /// </summary>
    public ZooKeeperDistributedLock CreateLock(string name) => 
        new(this._directoryPath, name, this._connectionString, this._options);

    IDistributedLock IDistributedLockProvider.CreateLock(string name) => this.CreateLock(name);

    /// <summary>
    /// Creates a <see cref="ZooKeeperDistributedReaderWriterLock"/> using the given <paramref name="name"/>.
    /// </summary>
    public ZooKeeperDistributedReaderWriterLock CreateReaderWriterLock(string name) => 
        new(this._directoryPath, name, this._connectionString, this._options);

    IDistributedReaderWriterLock IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string name) => this.CreateReaderWriterLock(name);

    /// <summary>
    /// Creates a <see cref="ZooKeeperDistributedSemaphore"/> using the given <paramref name="name"/> and <paramref name="maxCount"/>.
    /// </summary>
    public ZooKeeperDistributedSemaphore CreateSemaphore(string name, int maxCount) =>
        new(this._directoryPath, name, maxCount, this._connectionString, this._options);

    IDistributedSemaphore IDistributedSemaphoreProvider.CreateSemaphore(string name, int maxCount) => this.CreateSemaphore(name, maxCount);
}
