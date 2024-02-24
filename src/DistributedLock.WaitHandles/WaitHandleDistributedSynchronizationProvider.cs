namespace Medallion.Threading.WaitHandles;

/// <summary>
/// Implements <see cref="IDistributedLockProvider"/> for <see cref="EventWaitHandleDistributedLock"/>
/// and <see cref="IDistributedSemaphoreProvider"/> for <see cref="WaitHandleDistributedSemaphore"/>.
/// </summary>
public sealed class WaitHandleDistributedSynchronizationProvider : IDistributedLockProvider, IDistributedSemaphoreProvider
{
    private readonly TimeSpan? _abandonmentCheckCadence;

    /// <summary>
    /// Constructs a <see cref="WaitHandleDistributedSynchronizationProvider"/> using the provided <paramref name="abandonmentCheckCadence"/>.
    /// </summary>
    public WaitHandleDistributedSynchronizationProvider(TimeSpan? abandonmentCheckCadence = null)
    {
        this._abandonmentCheckCadence = abandonmentCheckCadence;
    }

    /// <summary>
    /// Creates a <see cref="EventWaitHandleDistributedLock"/> with the given <paramref name="name"/>. Unless
    /// <paramref name="exactName"/> is specified, invalid wait handle names will be escaped/hashed.
    /// </summary>
    public EventWaitHandleDistributedLock CreateLock(string name, bool exactName = false) =>
        new(name, this._abandonmentCheckCadence, exactName);

    IDistributedLock IDistributedLockProvider.CreateLock(string name) => this.CreateLock(name);

    /// <summary>
    /// Creates a <see cref="WaitHandleDistributedSemaphore"/> with the given <paramref name="name"/>
    /// and <paramref name="maxCount"/>. Unless <paramref name="exactName"/> is specified, invalid wait 
    /// handle names will be escaped/hashed.
    /// </summary>
    public WaitHandleDistributedSemaphore CreateSemaphore(string name, int maxCount, bool exactName = false) =>
        new(name, maxCount, this._abandonmentCheckCadence, exactName);

    IDistributedSemaphore IDistributedSemaphoreProvider.CreateSemaphore(string name, int maxCount) =>
        this.CreateSemaphore(name, maxCount);
}
