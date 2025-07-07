// AUTO-GENERATED

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
        CompositeDistributedSynchronizationHandle.TryAcquireAll(
            provider,
            static (p, n, t, c) => p.TryAcquireLock(n, t, c),
            names, timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedLock.Acquire(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static IDistributedSynchronizationHandle AcquireAllLocks(this IDistributedLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        CompositeDistributedSynchronizationHandle.AcquireAll(
            provider,
            static (p, n, t, c) => p.AcquireLock(n, t, c),
            names, timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedLock.TryAcquireAsync(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle?> TryAcquireAllLocksAsync(this IDistributedLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        CompositeDistributedSynchronizationHandle.TryAcquireAllAsync(
            provider,
            static (p, n, t, c) => p.TryAcquireLockAsync(n, t, c),
            names, timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedLock.AcquireAsync(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireAllLocksAsync(this IDistributedLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        CompositeDistributedSynchronizationHandle.AcquireAllAsync(
            provider,
            static (p, n, t, c) => p.AcquireLockAsync(n, t, c),
            names, timeout, cancellationToken);

    # endregion
}