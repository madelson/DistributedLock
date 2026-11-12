using Medallion.Threading.Internal;

namespace Medallion.Threading.InProcess;

/// <summary>
/// Implements a non-distributed, writer-preferring upgradeable reader-writer lock. Instances are created via
/// <see cref="InProcessDistributedSynchronizationProvider"/>, which shares the underlying state by scope and name.
/// A handle which is abandoned without being disposed is released only after it has been garbage collected and
/// processed by a background finalizer (roughly 30 seconds). Until then, an abandoned read handle blocks writers
/// and, once a writer is waiting, all new readers as well.
/// </summary>
public sealed partial class InProcessDistributedReaderWriterLock : IInternalDistributedUpgradeableReaderWriterLock<InProcessDistributedReaderWriterLockHandle, InProcessDistributedReaderWriterLockUpgradeableHandle>
{
    private readonly AsyncReaderWriterLockState _state;

    internal InProcessDistributedReaderWriterLock(string name, AsyncReaderWriterLockState state)
    {
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
        this._state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <summary>
    /// Implements <see cref="IDistributedReaderWriterLock.Name"/>
    /// </summary>
    public string Name { get; }

    async ValueTask<InProcessDistributedReaderWriterLockHandle?>
        IInternalDistributedReaderWriterLock<InProcessDistributedReaderWriterLockHandle>.InternalTryAcquireAsync(
            TimeoutValue timeout,
            CancellationToken cancellationToken,
            bool isWrite) =>
        await this._state.TryAcquireAsync(timeout, cancellationToken, isWrite).ConfigureAwait(false)
            ? new InProcessDistributedReaderWriterLockNonUpgradeableHandle(this._state, isWrite)
            : null;

    async ValueTask<InProcessDistributedReaderWriterLockUpgradeableHandle?>
        IInternalDistributedUpgradeableReaderWriterLock<InProcessDistributedReaderWriterLockHandle, InProcessDistributedReaderWriterLockUpgradeableHandle>
            .InternalTryAcquireUpgradeableReadLockAsync(TimeoutValue timeout, CancellationToken cancellationToken) =>
        await this._state.TryAcquireAsync(timeout, cancellationToken, InProcessReaderWriterLockType.Upgrade).ConfigureAwait(false)
            ? new InProcessDistributedReaderWriterLockUpgradeableHandle(this._state)
            : null;
}