// AUTO-GENERATED
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading;

/// <summary>
/// Productivity helper methods for <see cref="IDistributedUpgradeableReaderWriterLockProvider" />
/// </summary>
public static class DistributedUpgradeableReaderWriterLockProviderExtensions
{
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
}