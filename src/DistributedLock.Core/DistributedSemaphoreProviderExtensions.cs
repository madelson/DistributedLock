// AUTO-GENERATED

using Medallion.Threading.Internal;

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
        SyncViaAsync.Run(static s => s.provider.TryAcquireAllSemaphoresAsync(s.names, s.maxCount, s.timeout, s.cancellationToken), (provider, names, maxCount, timeout, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedSemaphore.Acquire(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static IDistributedSynchronizationHandle AcquireAllSemaphores(this IDistributedSemaphoreProvider provider, IReadOnlyList<string> names, int maxCount, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(static s => s.provider.AcquireAllSemaphoresAsync(s.names, s.maxCount, s.timeout, s.cancellationToken), (provider, names, maxCount, timeout, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedSemaphore.TryAcquireAsync(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle?> TryAcquireAllSemaphoresAsync(this IDistributedSemaphoreProvider provider, IReadOnlyList<string> names, int maxCount, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        provider.TryAcquireAllSemaphoresInternalAsync(names, maxCount, timeout, cancellationToken).GetHandleOrDefault();

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedSemaphore.AcquireAsync(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireAllSemaphoresAsync(this IDistributedSemaphoreProvider provider, IReadOnlyList<string> names, int maxCount, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        provider.TryAcquireAllSemaphoresInternalAsync(names, maxCount, timeout, cancellationToken).GetHandleOrTimeout();

    # endregion
}