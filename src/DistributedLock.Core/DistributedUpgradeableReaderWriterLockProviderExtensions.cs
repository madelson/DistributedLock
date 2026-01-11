// AUTO-GENERATED

using Medallion.Threading.Internal;

namespace Medallion.Threading;

/// <summary>
/// Productivity helper methods for <see cref="IDistributedUpgradeableReaderWriterLockProvider" />
/// </summary>
public static class DistributedUpgradeableReaderWriterLockProviderExtensions
{
    # region Single Lock Methods

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedUpgradeableReaderWriterLockProvider.CreateUpgradeableReaderWriterLock(string)" /> and then
    /// <see cref="IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLock(TimeSpan, CancellationToken)" />.
    /// </summary>
    public static IDistributedLockUpgradeableHandle? TryAcquireUpgradeableReadLock(this IDistributedUpgradeableReaderWriterLockProvider provider, string name, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateUpgradeableReaderWriterLock(name).TryAcquireUpgradeableReadLock(timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedUpgradeableReaderWriterLockProvider.CreateUpgradeableReaderWriterLock(string)" /> and then
    /// <see cref="IDistributedUpgradeableReaderWriterLock.AcquireUpgradeableReadLock(TimeSpan?, CancellationToken)" />.
    /// </summary>
    public static IDistributedLockUpgradeableHandle AcquireUpgradeableReadLock(this IDistributedUpgradeableReaderWriterLockProvider provider, string name, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateUpgradeableReaderWriterLock(name).AcquireUpgradeableReadLock(timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedUpgradeableReaderWriterLockProvider.CreateUpgradeableReaderWriterLock(string)" /> and then
    /// <see cref="IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLockAsync(TimeSpan, CancellationToken)" />.
    /// </summary>
    public static ValueTask<IDistributedLockUpgradeableHandle?> TryAcquireUpgradeableReadLockAsync(this IDistributedUpgradeableReaderWriterLockProvider provider, string name, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateUpgradeableReaderWriterLock(name).TryAcquireUpgradeableReadLockAsync(timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedUpgradeableReaderWriterLockProvider.CreateUpgradeableReaderWriterLock(string)" /> and then
    /// <see cref="IDistributedUpgradeableReaderWriterLock.AcquireUpgradeableReadLockAsync(TimeSpan?, CancellationToken)" />.
    /// </summary>
    public static ValueTask<IDistributedLockUpgradeableHandle> AcquireUpgradeableReadLockAsync(this IDistributedUpgradeableReaderWriterLockProvider provider, string name, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateUpgradeableReaderWriterLock(name).AcquireUpgradeableReadLockAsync(timeout, cancellationToken);

    # endregion

    # region Composite Lock Methods

    // Composite methods are not supported for IDistributedUpgradeableReaderWriterLock
    // because a composite acquire operation must be able to roll back and upgrade does not support that.

    # endregion
}