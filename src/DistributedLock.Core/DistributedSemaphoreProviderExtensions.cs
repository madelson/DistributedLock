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
    /// <param name="provider">The provider used to create each semaphore.</param>
    /// <param name="names">The names of the semaphores to acquire.</param>
    /// <param name="maxCount">The max count of each semaphore.</param>
    /// <param name="timeout">The total time to wait for all semaphores to be acquired.</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static IDistributedSynchronizationHandle? TryAcquireAllSemaphores(this IDistributedSemaphoreProvider provider, IReadOnlyList<string> names, int maxCount, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(static s => s.provider.TryAcquireAllSemaphoresAsync(s.names, s.maxCount, s.timeout, s.cancellationToken), (provider, names, maxCount, timeout, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedSemaphore.TryAcquire(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each semaphore.</param>
    /// <param name="names">The names of the semaphores to acquire.</param>
    /// <param name="maxCount">The max count of each semaphore.</param>
    /// <param name="timeout">The total time to wait for all semaphores to be acquired.</param>
    /// <param name="observeLockLostToken">
    /// Whether to observe the <see cref="IDistributedSynchronizationHandle.HandleLostToken"/> of already-acquired handles while acquiring the remaining ones.
    /// If a handle is lost mid-acquisition, all acquired handles are released and null is returned.
    /// Has no effect when only one name is given. Note that accessing the token may start background monitoring for some providers.
    /// </param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static IDistributedSynchronizationHandle? TryAcquireAllSemaphores(this IDistributedSemaphoreProvider provider, IReadOnlyList<string> names, int maxCount, TimeSpan timeout, bool observeLockLostToken, CancellationToken cancellationToken) =>
        SyncViaAsync.Run(static s => s.provider.TryAcquireAllSemaphoresAsync(s.names, s.maxCount, s.timeout, s.observeLockLostToken, s.cancellationToken), (provider, names, maxCount, timeout, observeLockLostToken, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedSemaphore.Acquire(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each semaphore.</param>
    /// <param name="names">The names of the semaphores to acquire.</param>
    /// <param name="maxCount">The max count of each semaphore.</param>
    /// <param name="timeout">The total time to wait for all semaphores to be acquired.</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static IDistributedSynchronizationHandle AcquireAllSemaphores(this IDistributedSemaphoreProvider provider, IReadOnlyList<string> names, int maxCount, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(static s => s.provider.AcquireAllSemaphoresAsync(s.names, s.maxCount, s.timeout, s.cancellationToken), (provider, names, maxCount, timeout, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedSemaphore.Acquire(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each semaphore.</param>
    /// <param name="names">The names of the semaphores to acquire.</param>
    /// <param name="maxCount">The max count of each semaphore.</param>
    /// <param name="timeout">The total time to wait for all semaphores to be acquired.</param>
    /// <param name="observeLockLostToken">
    /// Whether to observe the <see cref="IDistributedSynchronizationHandle.HandleLostToken"/> of already-acquired handles while acquiring the remaining ones.
    /// If a handle is lost mid-acquisition, all acquired handles are released and a <see cref="DistributedLockLostException"/> is thrown.
    /// Has no effect when only one name is given. Note that accessing the token may start background monitoring for some providers.
    /// </param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static IDistributedSynchronizationHandle AcquireAllSemaphores(this IDistributedSemaphoreProvider provider, IReadOnlyList<string> names, int maxCount, TimeSpan? timeout, bool observeLockLostToken, CancellationToken cancellationToken) =>
        SyncViaAsync.Run(static s => s.provider.AcquireAllSemaphoresAsync(s.names, s.maxCount, s.timeout, s.observeLockLostToken, s.cancellationToken), (provider, names, maxCount, timeout, observeLockLostToken, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedSemaphore.TryAcquireAsync(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each semaphore.</param>
    /// <param name="names">The names of the semaphores to acquire.</param>
    /// <param name="maxCount">The max count of each semaphore.</param>
    /// <param name="timeout">The total time to wait for all semaphores to be acquired.</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static ValueTask<IDistributedSynchronizationHandle?> TryAcquireAllSemaphoresAsync(this IDistributedSemaphoreProvider provider, IReadOnlyList<string> names, int maxCount, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        provider.TryAcquireAllSemaphoresInternalAsync(names, maxCount, timeout, observeLockLostToken: false, cancellationToken).GetHandleOrDefault();

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedSemaphore.TryAcquireAsync(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each semaphore.</param>
    /// <param name="names">The names of the semaphores to acquire.</param>
    /// <param name="maxCount">The max count of each semaphore.</param>
    /// <param name="timeout">The total time to wait for all semaphores to be acquired.</param>
    /// <param name="observeLockLostToken">
    /// Whether to observe the <see cref="IDistributedSynchronizationHandle.HandleLostToken"/> of already-acquired handles while acquiring the remaining ones.
    /// If a handle is lost mid-acquisition, all acquired handles are released and null is returned.
    /// Has no effect when only one name is given. Note that accessing the token may start background monitoring for some providers.
    /// </param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static ValueTask<IDistributedSynchronizationHandle?> TryAcquireAllSemaphoresAsync(this IDistributedSemaphoreProvider provider, IReadOnlyList<string> names, int maxCount, TimeSpan timeout, bool observeLockLostToken, CancellationToken cancellationToken) =>
        provider.TryAcquireAllSemaphoresInternalAsync(names, maxCount, timeout, observeLockLostToken, cancellationToken).GetHandleOrDefault();

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedSemaphore.AcquireAsync(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each semaphore.</param>
    /// <param name="names">The names of the semaphores to acquire.</param>
    /// <param name="maxCount">The max count of each semaphore.</param>
    /// <param name="timeout">The total time to wait for all semaphores to be acquired.</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireAllSemaphoresAsync(this IDistributedSemaphoreProvider provider, IReadOnlyList<string> names, int maxCount, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        provider.TryAcquireAllSemaphoresInternalAsync(names, maxCount, timeout, observeLockLostToken: false, cancellationToken).GetHandleOrTimeout();

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedSemaphore.AcquireAsync(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each semaphore.</param>
    /// <param name="names">The names of the semaphores to acquire.</param>
    /// <param name="maxCount">The max count of each semaphore.</param>
    /// <param name="timeout">The total time to wait for all semaphores to be acquired.</param>
    /// <param name="observeLockLostToken">
    /// Whether to observe the <see cref="IDistributedSynchronizationHandle.HandleLostToken"/> of already-acquired handles while acquiring the remaining ones.
    /// If a handle is lost mid-acquisition, all acquired handles are released and a <see cref="DistributedLockLostException"/> is thrown.
    /// Has no effect when only one name is given. Note that accessing the token may start background monitoring for some providers.
    /// </param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireAllSemaphoresAsync(this IDistributedSemaphoreProvider provider, IReadOnlyList<string> names, int maxCount, TimeSpan? timeout, bool observeLockLostToken, CancellationToken cancellationToken) =>
        provider.TryAcquireAllSemaphoresInternalAsync(names, maxCount, timeout, observeLockLostToken, cancellationToken).GetHandleOrTimeout();

    # endregion
}