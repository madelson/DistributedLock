// AUTO-GENERATED

using Medallion.Threading.Internal;
using System.Threading;

namespace Medallion.Threading;

/// <summary>
/// Productivity helper methods for <see cref="IDistributedLockProvider" />
/// </summary>
public static class DistributedLockProviderExtensions
{
    # region Single Lock Methods

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> and then
    /// <see cref="IDistributedLock.TryAcquire(TimeSpan, CancellationToken)" />.
    /// </summary>
    public static IDistributedSynchronizationHandle? TryAcquireLock(this IDistributedLockProvider provider, string name, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateLock(name).TryAcquire(timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> and then
    /// <see cref="IDistributedLock.Acquire(TimeSpan?, CancellationToken)" />.
    /// </summary>
    public static IDistributedSynchronizationHandle AcquireLock(this IDistributedLockProvider provider, string name, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateLock(name).Acquire(timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> and then
    /// <see cref="IDistributedLock.TryAcquireAsync(TimeSpan, CancellationToken)" />.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle?> TryAcquireLockAsync(this IDistributedLockProvider provider, string name, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateLock(name).TryAcquireAsync(timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> and then
    /// <see cref="IDistributedLock.AcquireAsync(TimeSpan?, CancellationToken)" />.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireLockAsync(this IDistributedLockProvider provider, string name, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateLock(name).AcquireAsync(timeout, cancellationToken);

    # endregion

    # region Composite Lock Methods

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedLock.TryAcquire(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static IDistributedSynchronizationHandle? TryAcquireAllLocks(this IDistributedLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(
            static s => s.provider.TryAcquireAllLocksAsync(s.names, s.timeout, s.cancellationToken),
            (provider, names, timeout, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedLock.Acquire(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static IDistributedSynchronizationHandle AcquireAllLocks(this IDistributedLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(
            static s => s.provider.AcquireAllLocksAsync(s.names, s.timeout, s.cancellationToken),
            (provider, names, timeout, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedLock.TryAcquireAsync(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle?> TryAcquireAllLocksAsync(this IDistributedLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        provider.TryAcquireAllLocksInternalAsync(names, timeout, cancellationToken).GetHandleOrDefault();

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedLock.AcquireAsync(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireAllLocksAsync(this IDistributedLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        provider.TryAcquireAllLocksInternalAsync(names, timeout ?? Timeout.InfiniteTimeSpan, cancellationToken).GetHandleOrTimeout();

    private static ValueTask<CompositeDistributedSynchronizationHandle.AcquireResult> TryAcquireAllLocksInternalAsync(
        this IDistributedLockProvider provider,
        IReadOnlyList<string> names,
        TimeSpan timeout,
        CancellationToken cancellationToken) =>
        CompositeDistributedSynchronizationHandle.TryAcquireAllAsync(
            CompositeDistributedSynchronizationHandle.FromNames(names, provider ?? throw new ArgumentNullException(nameof(provider)), static (p, n) => p.CreateLock(n)),
            static (p, t, c) => SyncViaAsync.IsSynchronous ? p.TryAcquire(t, c).AsValueTask() : p.TryAcquireAsync(t, c),
            timeout, cancellationToken);

    # endregion
}