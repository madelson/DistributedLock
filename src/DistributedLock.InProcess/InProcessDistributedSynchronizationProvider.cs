namespace Medallion.Threading.InProcess;

/// <summary>
/// Implements <see cref="IDistributedLockProvider"/>, <see cref="IDistributedSemaphoreProvider"/>, and
/// <see cref="IDistributedUpgradeableReaderWriterLockProvider"/> for in-process synchronization primitives.
/// 
/// Primitives are cached process-wide by (<see cref="Scope"/>, name), so all primitives created for the same name
/// by providers with the same <see cref="Scope"/> coordinate with one another, even across provider instances.
/// Providers with different scopes never coordinate.
/// 
/// Locks, semaphores, and reader-writer locks are cached separately: a lock and a reader-writer lock with the
/// same name do NOT coordinate with one another.
/// 
/// Note that these primitives only synchronize within the current process. Handles which are never disposed are
/// released by a background finalizer only after the handle has been garbage collected, which can take on the order
/// of 30 seconds; until then, the primitive remains held.
/// </summary>
public sealed class InProcessDistributedSynchronizationProvider :
    IDistributedLockProvider, IDistributedSemaphoreProvider, IDistributedUpgradeableReaderWriterLockProvider
{
    private static readonly ConcurrentWeakReferenceDictionary<(string Scope, string Name), SemaphoreSlim> Locks = new();
    private static readonly ConcurrentWeakReferenceDictionary<(string Scope, string Name), SemaphoreSlim> Semaphores = new();
    private static readonly ConcurrentWeakReferenceDictionary<(string Scope, string Name), AsyncReaderWriterLockState> ReaderWriterLocks = new();

    /// <summary>
    /// Constructs a provider for the given <paramref name="scope"/>. Providers with the same scope share primitives;
    /// providers with different scopes are isolated from one another. When <paramref name="scope"/> is not specified,
    /// a default process-wide scope is used.
    /// </summary>
    public InProcessDistributedSynchronizationProvider(string? scope = null)
    {
        this.Scope = scope ?? string.Empty;
    }

    /// <summary>
    /// The scope within which primitives created by this provider are shared.
    /// </summary>
    public string Scope { get; }

    /// <summary>
    /// Creates an <see cref="InProcessDistributedLock"/> with the given <paramref name="name"/>.
    /// </summary>
    public InProcessDistributedLock CreateLock(string name) =>
        new(name, Locks.GetOrAdd(this.GetKey(name), _ => new SemaphoreSlim(1, 1)));

    IDistributedLock IDistributedLockProvider.CreateLock(string name) => this.CreateLock(name);

    /// <summary>
    /// Creates an <see cref="InProcessDistributedReaderWriterLock"/> with the given <paramref name="name"/>.
    /// </summary>
    public InProcessDistributedReaderWriterLock CreateReaderWriterLock(string name) =>
        new(name, ReaderWriterLocks.GetOrAdd(this.GetKey(name), _ => new AsyncReaderWriterLockState()));

    IDistributedReaderWriterLock IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string name) =>
        this.CreateReaderWriterLock(name);

    IDistributedUpgradeableReaderWriterLock IDistributedUpgradeableReaderWriterLockProvider.CreateUpgradeableReaderWriterLock(string name) =>
        this.CreateReaderWriterLock(name);

    /// <summary>
    /// Creates an <see cref="InProcessDistributedSemaphore"/> with the given <paramref name="name"/> and <paramref name="maxCount"/>.
    /// </summary>
    public InProcessDistributedSemaphore CreateSemaphore(string name, int maxCount)
    {
        if (maxCount < 1) { throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "must be positive"); }
        return new(name, maxCount, Semaphores.GetOrAdd(this.GetKey(name), _ => new SemaphoreSlim(maxCount, maxCount)));
    }

    IDistributedSemaphore IDistributedSemaphoreProvider.CreateSemaphore(string name, int maxCount) =>
        this.CreateSemaphore(name, maxCount);

    private (string Scope, string Name) GetKey(string name) =>
    (this.Scope, name ?? throw new ArgumentNullException(nameof(name)));
}
