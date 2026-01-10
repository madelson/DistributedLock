// AUTO-GENERATED

namespace Medallion.Threading;

/// <summary>
/// Productivity helper methods for <see cref="IDistributedReaderWriterLockProvider" />
/// </summary>
public static class DistributedReaderWriterLockProviderExtensions
{
    # region Single Lock Methods

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> and then
    /// <see cref="IDistributedReaderWriterLock.TryAcquireReadLock(TimeSpan, CancellationToken)" />.
    /// </summary>
    public static IDistributedSynchronizationHandle? TryAcquireReadLock(this IDistributedReaderWriterLockProvider provider, string name, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateReaderWriterLock(name).TryAcquireReadLock(timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> and then
    /// <see cref="IDistributedReaderWriterLock.AcquireReadLock(TimeSpan?, CancellationToken)" />.
    /// </summary>
    public static IDistributedSynchronizationHandle AcquireReadLock(this IDistributedReaderWriterLockProvider provider, string name, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateReaderWriterLock(name).AcquireReadLock(timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> and then
    /// <see cref="IDistributedReaderWriterLock.TryAcquireReadLockAsync(TimeSpan, CancellationToken)" />.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle?> TryAcquireReadLockAsync(this IDistributedReaderWriterLockProvider provider, string name, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateReaderWriterLock(name).TryAcquireReadLockAsync(timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> and then
    /// <see cref="IDistributedReaderWriterLock.AcquireReadLockAsync(TimeSpan?, CancellationToken)" />.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireReadLockAsync(this IDistributedReaderWriterLockProvider provider, string name, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateReaderWriterLock(name).AcquireReadLockAsync(timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> and then
    /// <see cref="IDistributedReaderWriterLock.TryAcquireWriteLock(TimeSpan, CancellationToken)" />.
    /// </summary>
    public static IDistributedSynchronizationHandle? TryAcquireWriteLock(this IDistributedReaderWriterLockProvider provider, string name, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateReaderWriterLock(name).TryAcquireWriteLock(timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> and then
    /// <see cref="IDistributedReaderWriterLock.AcquireWriteLock(TimeSpan?, CancellationToken)" />.
    /// </summary>
    public static IDistributedSynchronizationHandle AcquireWriteLock(this IDistributedReaderWriterLockProvider provider, string name, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateReaderWriterLock(name).AcquireWriteLock(timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> and then
    /// <see cref="IDistributedReaderWriterLock.TryAcquireWriteLockAsync(TimeSpan, CancellationToken)" />.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle?> TryAcquireWriteLockAsync(this IDistributedReaderWriterLockProvider provider, string name, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateReaderWriterLock(name).TryAcquireWriteLockAsync(timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> and then
    /// <see cref="IDistributedReaderWriterLock.AcquireWriteLockAsync(TimeSpan?, CancellationToken)" />.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireWriteLockAsync(this IDistributedReaderWriterLockProvider provider, string name, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateReaderWriterLock(name).AcquireWriteLockAsync(timeout, cancellationToken);

    # endregion

    # region Composite Lock Methods

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.TryAcquireReadLock(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static IDistributedSynchronizationHandle? TryAcquireAllReadLocks(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        CompositeDistributedSynchronizationHandle.TryAcquireAll(
            provider,
            static (p, n, t, c) => p.TryAcquireReadLock(n, t, c),
            names, timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.AcquireReadLock(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static IDistributedSynchronizationHandle AcquireAllReadLocks(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        CompositeDistributedSynchronizationHandle.AcquireAll(
            provider,
            static (p, n, t, c) => p.AcquireReadLock(n, t, c),
            names, timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.TryAcquireReadLockAsync(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle?> TryAcquireAllReadLocksAsync(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        CompositeDistributedSynchronizationHandle.TryAcquireAllAsync(
            provider,
            static (p, n, t, c) => p.TryAcquireReadLockAsync(n, t, c),
            names, timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.AcquireReadLockAsync(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireAllReadLocksAsync(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        CompositeDistributedSynchronizationHandle.AcquireAllAsync(
            provider,
            static (p, n, t, c) => p.AcquireReadLockAsync(n, t, c),
            names, timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.TryAcquireWriteLock(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static IDistributedSynchronizationHandle? TryAcquireAllWriteLocks(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        CompositeDistributedSynchronizationHandle.TryAcquireAll(
            provider,
            static (p, n, t, c) => p.TryAcquireWriteLock(n, t, c),
            names, timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.AcquireWriteLock(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static IDistributedSynchronizationHandle AcquireAllWriteLocks(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        CompositeDistributedSynchronizationHandle.AcquireAll(
            provider,
            static (p, n, t, c) => p.AcquireWriteLock(n, t, c),
            names, timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.TryAcquireWriteLockAsync(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle?> TryAcquireAllWriteLocksAsync(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        CompositeDistributedSynchronizationHandle.TryAcquireAllAsync(
            provider,
            static (p, n, t, c) => p.TryAcquireWriteLockAsync(n, t, c),
            names, timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.AcquireWriteLockAsync(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireAllWriteLocksAsync(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        CompositeDistributedSynchronizationHandle.AcquireAllAsync(
            provider,
            static (p, n, t, c) => p.AcquireWriteLockAsync(n, t, c),
            names, timeout, cancellationToken);

    # endregion
}