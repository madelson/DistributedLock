// AUTO-GENERATED

namespace Medallion.Threading;

/// <summary>
/// Productivity helper methods for <see cref="IDistributedSemaphoreProvider" />
/// </summary>
public static class DistributedSemaphoreProviderExtensions
{
    # region Single Lock Methods

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> and then
    /// <see cref="IDistributedSemaphore.TryAcquire(TimeSpan, CancellationToken)" />.
    /// </summary>
    public static IDistributedSynchronizationHandle? TryAcquireSemaphore(this IDistributedSemaphoreProvider provider, string name, int maxCount, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateSemaphore(name, maxCount).TryAcquire(timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> and then
    /// <see cref="IDistributedSemaphore.Acquire(TimeSpan?, CancellationToken)" />.
    /// </summary>
    public static IDistributedSynchronizationHandle AcquireSemaphore(this IDistributedSemaphoreProvider provider, string name, int maxCount, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateSemaphore(name, maxCount).Acquire(timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> and then
    /// <see cref="IDistributedSemaphore.TryAcquireAsync(TimeSpan, CancellationToken)" />.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle?> TryAcquireSemaphoreAsync(this IDistributedSemaphoreProvider provider, string name, int maxCount, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateSemaphore(name, maxCount).TryAcquireAsync(timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> and then
    /// <see cref="IDistributedSemaphore.AcquireAsync(TimeSpan?, CancellationToken)" />.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireSemaphoreAsync(this IDistributedSemaphoreProvider provider, string name, int maxCount, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).CreateSemaphore(name, maxCount).AcquireAsync(timeout, cancellationToken);

    # endregion

    # region Composite Lock Methods

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedSemaphore.TryAcquire(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static IDistributedSynchronizationHandle? TryAcquireAllSemaphores(this IDistributedSemaphoreProvider provider, IReadOnlyList<string> names, int maxCount, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        CompositeDistributedSynchronizationHandle.TryAcquireAll(
            provider,
            static (p, n, mc, t, c) => p.TryAcquireSemaphore(n, mc, t, c),
            names, maxCount, timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedSemaphore.Acquire(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static IDistributedSynchronizationHandle AcquireAllSemaphores(this IDistributedSemaphoreProvider provider, IReadOnlyList<string> names, int maxCount, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        CompositeDistributedSynchronizationHandle.AcquireAll(
            provider,
            static (p, n, mc, t, c) => p.AcquireSemaphore(n, mc, t, c),
            names, maxCount, timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedSemaphore.TryAcquireAsync(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle?> TryAcquireAllSemaphoresAsync(this IDistributedSemaphoreProvider provider, IReadOnlyList<string> names, int maxCount, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        CompositeDistributedSynchronizationHandle.TryAcquireAllAsync(
            provider,
            static (p, n, mc, t, c) => p.TryAcquireSemaphoreAsync(n, mc, t, c),
            names, maxCount, timeout, cancellationToken);

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedSemaphore.AcquireAsync(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireAllSemaphoresAsync(this IDistributedSemaphoreProvider provider, IReadOnlyList<string> names, int maxCount, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        CompositeDistributedSynchronizationHandle.AcquireAllAsync(
            provider,
            static (p, n, mc, t, c) => p.AcquireSemaphoreAsync(n, mc, t, c),
            names, maxCount, timeout, cancellationToken);

    # endregion
}