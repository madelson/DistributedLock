// AUTO-GENERATED

using Medallion.Threading.Internal;

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
    /// <param name="provider">The provider used to create each lock.</param>
    /// <param name="names">The names of the locks to acquire.</param>
    /// <param name="timeout">The total time to wait for all locks to be acquired.</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static IDistributedSynchronizationHandle? TryAcquireAllLocks(this IDistributedLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(static s => s.provider.TryAcquireAllLocksAsync(s.names, s.timeout, s.cancellationToken), (provider, names, timeout, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedLock.TryAcquire(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each lock.</param>
    /// <param name="names">The names of the locks to acquire.</param>
    /// <param name="timeout">The total time to wait for all locks to be acquired.</param>
    /// <param name="observeLockLostToken">
    /// Whether to observe the <see cref="IDistributedSynchronizationHandle.HandleLostToken"/> of already-acquired handles while acquiring the remaining ones.
    /// If a handle is lost mid-acquisition, all acquired handles are released and null is returned.
    /// Has no effect when only one name is given. Note that accessing the token may start background monitoring for some providers.
    /// </param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static IDistributedSynchronizationHandle? TryAcquireAllLocks(this IDistributedLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout, bool observeLockLostToken, CancellationToken cancellationToken) =>
        SyncViaAsync.Run(static s => s.provider.TryAcquireAllLocksAsync(s.names, s.timeout, s.observeLockLostToken, s.cancellationToken), (provider, names, timeout, observeLockLostToken, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedLock.Acquire(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each lock.</param>
    /// <param name="names">The names of the locks to acquire.</param>
    /// <param name="timeout">The total time to wait for all locks to be acquired.</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static IDistributedSynchronizationHandle AcquireAllLocks(this IDistributedLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(static s => s.provider.AcquireAllLocksAsync(s.names, s.timeout, s.cancellationToken), (provider, names, timeout, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedLock.Acquire(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each lock.</param>
    /// <param name="names">The names of the locks to acquire.</param>
    /// <param name="timeout">The total time to wait for all locks to be acquired.</param>
    /// <param name="observeLockLostToken">
    /// Whether to observe the <see cref="IDistributedSynchronizationHandle.HandleLostToken"/> of already-acquired handles while acquiring the remaining ones.
    /// If a handle is lost mid-acquisition, all acquired handles are released and a <see cref="DistributedLockLostException"/> is thrown.
    /// Has no effect when only one name is given. Note that accessing the token may start background monitoring for some providers.
    /// </param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static IDistributedSynchronizationHandle AcquireAllLocks(this IDistributedLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout, bool observeLockLostToken, CancellationToken cancellationToken) =>
        SyncViaAsync.Run(static s => s.provider.AcquireAllLocksAsync(s.names, s.timeout, s.observeLockLostToken, s.cancellationToken), (provider, names, timeout, observeLockLostToken, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedLock.TryAcquireAsync(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each lock.</param>
    /// <param name="names">The names of the locks to acquire.</param>
    /// <param name="timeout">The total time to wait for all locks to be acquired.</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static ValueTask<IDistributedSynchronizationHandle?> TryAcquireAllLocksAsync(this IDistributedLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        provider.TryAcquireAllLocksInternalAsync(names, timeout, observeLockLostToken: false, cancellationToken).GetHandleOrDefault();

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedLock.TryAcquireAsync(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each lock.</param>
    /// <param name="names">The names of the locks to acquire.</param>
    /// <param name="timeout">The total time to wait for all locks to be acquired.</param>
    /// <param name="observeLockLostToken">
    /// Whether to observe the <see cref="IDistributedSynchronizationHandle.HandleLostToken"/> of already-acquired handles while acquiring the remaining ones.
    /// If a handle is lost mid-acquisition, all acquired handles are released and null is returned.
    /// Has no effect when only one name is given. Note that accessing the token may start background monitoring for some providers.
    /// </param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static ValueTask<IDistributedSynchronizationHandle?> TryAcquireAllLocksAsync(this IDistributedLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout, bool observeLockLostToken, CancellationToken cancellationToken) =>
        provider.TryAcquireAllLocksInternalAsync(names, timeout, observeLockLostToken, cancellationToken).GetHandleOrDefault();

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedLock.AcquireAsync(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each lock.</param>
    /// <param name="names">The names of the locks to acquire.</param>
    /// <param name="timeout">The total time to wait for all locks to be acquired.</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireAllLocksAsync(this IDistributedLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        provider.TryAcquireAllLocksInternalAsync(names, timeout, observeLockLostToken: false, cancellationToken).GetHandleOrTimeout();

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedLock.AcquireAsync(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each lock.</param>
    /// <param name="names">The names of the locks to acquire.</param>
    /// <param name="timeout">The total time to wait for all locks to be acquired.</param>
    /// <param name="observeLockLostToken">
    /// Whether to observe the <see cref="IDistributedSynchronizationHandle.HandleLostToken"/> of already-acquired handles while acquiring the remaining ones.
    /// If a handle is lost mid-acquisition, all acquired handles are released and a <see cref="DistributedLockLostException"/> is thrown.
    /// Has no effect when only one name is given. Note that accessing the token may start background monitoring for some providers.
    /// </param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireAllLocksAsync(this IDistributedLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout, bool observeLockLostToken, CancellationToken cancellationToken) =>
        provider.TryAcquireAllLocksInternalAsync(names, timeout, observeLockLostToken, cancellationToken).GetHandleOrTimeout();

    # endregion
}