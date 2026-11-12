// AUTO-GENERATED

using Medallion.Threading.Internal;

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
    /// <param name="provider">The provider used to create each reader-writer lock.</param>
    /// <param name="names">The names of the reader-writer locks on which to acquire read locks.</param>
    /// <param name="timeout">The total time to wait for all read locks to be acquired.</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static IDistributedSynchronizationHandle? TryAcquireAllReadLocks(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(static s => s.provider.TryAcquireAllReadLocksAsync(s.names, s.timeout, s.cancellationToken), (provider, names, timeout, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.TryAcquireReadLock(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each reader-writer lock.</param>
    /// <param name="names">The names of the reader-writer locks on which to acquire read locks.</param>
    /// <param name="timeout">The total time to wait for all read locks to be acquired.</param>
    /// <param name="observeLockLostToken">
    /// Whether to observe the <see cref="IDistributedSynchronizationHandle.HandleLostToken"/> of already-acquired handles while acquiring the remaining ones.
    /// If a handle is lost mid-acquisition, all acquired handles are released and null is returned.
    /// Has no effect when only one name is given. Note that accessing the token may start background monitoring for some providers.
    /// </param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static IDistributedSynchronizationHandle? TryAcquireAllReadLocks(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout, bool observeLockLostToken, CancellationToken cancellationToken) =>
        SyncViaAsync.Run(static s => s.provider.TryAcquireAllReadLocksAsync(s.names, s.timeout, s.observeLockLostToken, s.cancellationToken), (provider, names, timeout, observeLockLostToken, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.AcquireReadLock(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each reader-writer lock.</param>
    /// <param name="names">The names of the reader-writer locks on which to acquire read locks.</param>
    /// <param name="timeout">The total time to wait for all read locks to be acquired.</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static IDistributedSynchronizationHandle AcquireAllReadLocks(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(static s => s.provider.AcquireAllReadLocksAsync(s.names, s.timeout, s.cancellationToken), (provider, names, timeout, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.AcquireReadLock(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each reader-writer lock.</param>
    /// <param name="names">The names of the reader-writer locks on which to acquire read locks.</param>
    /// <param name="timeout">The total time to wait for all read locks to be acquired.</param>
    /// <param name="observeLockLostToken">
    /// Whether to observe the <see cref="IDistributedSynchronizationHandle.HandleLostToken"/> of already-acquired handles while acquiring the remaining ones.
    /// If a handle is lost mid-acquisition, all acquired handles are released and a <see cref="DistributedLockLostException"/> is thrown.
    /// Has no effect when only one name is given. Note that accessing the token may start background monitoring for some providers.
    /// </param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static IDistributedSynchronizationHandle AcquireAllReadLocks(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout, bool observeLockLostToken, CancellationToken cancellationToken) =>
        SyncViaAsync.Run(static s => s.provider.AcquireAllReadLocksAsync(s.names, s.timeout, s.observeLockLostToken, s.cancellationToken), (provider, names, timeout, observeLockLostToken, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.TryAcquireReadLockAsync(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each reader-writer lock.</param>
    /// <param name="names">The names of the reader-writer locks on which to acquire read locks.</param>
    /// <param name="timeout">The total time to wait for all read locks to be acquired.</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static ValueTask<IDistributedSynchronizationHandle?> TryAcquireAllReadLocksAsync(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        provider.TryAcquireAllReadLocksInternalAsync(names, timeout, observeLockLostToken: false, cancellationToken).GetHandleOrDefault();

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.TryAcquireReadLockAsync(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each reader-writer lock.</param>
    /// <param name="names">The names of the reader-writer locks on which to acquire read locks.</param>
    /// <param name="timeout">The total time to wait for all read locks to be acquired.</param>
    /// <param name="observeLockLostToken">
    /// Whether to observe the <see cref="IDistributedSynchronizationHandle.HandleLostToken"/> of already-acquired handles while acquiring the remaining ones.
    /// If a handle is lost mid-acquisition, all acquired handles are released and null is returned.
    /// Has no effect when only one name is given. Note that accessing the token may start background monitoring for some providers.
    /// </param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static ValueTask<IDistributedSynchronizationHandle?> TryAcquireAllReadLocksAsync(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout, bool observeLockLostToken, CancellationToken cancellationToken) =>
        provider.TryAcquireAllReadLocksInternalAsync(names, timeout, observeLockLostToken, cancellationToken).GetHandleOrDefault();

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.AcquireReadLockAsync(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each reader-writer lock.</param>
    /// <param name="names">The names of the reader-writer locks on which to acquire read locks.</param>
    /// <param name="timeout">The total time to wait for all read locks to be acquired.</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireAllReadLocksAsync(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        provider.TryAcquireAllReadLocksInternalAsync(names, timeout, observeLockLostToken: false, cancellationToken).GetHandleOrTimeout();

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.AcquireReadLockAsync(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each reader-writer lock.</param>
    /// <param name="names">The names of the reader-writer locks on which to acquire read locks.</param>
    /// <param name="timeout">The total time to wait for all read locks to be acquired.</param>
    /// <param name="observeLockLostToken">
    /// Whether to observe the <see cref="IDistributedSynchronizationHandle.HandleLostToken"/> of already-acquired handles while acquiring the remaining ones.
    /// If a handle is lost mid-acquisition, all acquired handles are released and a <see cref="DistributedLockLostException"/> is thrown.
    /// Has no effect when only one name is given. Note that accessing the token may start background monitoring for some providers.
    /// </param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireAllReadLocksAsync(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout, bool observeLockLostToken, CancellationToken cancellationToken) =>
        provider.TryAcquireAllReadLocksInternalAsync(names, timeout, observeLockLostToken, cancellationToken).GetHandleOrTimeout();

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.TryAcquireWriteLock(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each reader-writer lock.</param>
    /// <param name="names">The names of the reader-writer locks on which to acquire write locks.</param>
    /// <param name="timeout">The total time to wait for all write locks to be acquired.</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static IDistributedSynchronizationHandle? TryAcquireAllWriteLocks(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(static s => s.provider.TryAcquireAllWriteLocksAsync(s.names, s.timeout, s.cancellationToken), (provider, names, timeout, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.TryAcquireWriteLock(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each reader-writer lock.</param>
    /// <param name="names">The names of the reader-writer locks on which to acquire write locks.</param>
    /// <param name="timeout">The total time to wait for all write locks to be acquired.</param>
    /// <param name="observeLockLostToken">
    /// Whether to observe the <see cref="IDistributedSynchronizationHandle.HandleLostToken"/> of already-acquired handles while acquiring the remaining ones.
    /// If a handle is lost mid-acquisition, all acquired handles are released and null is returned.
    /// Has no effect when only one name is given. Note that accessing the token may start background monitoring for some providers.
    /// </param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static IDistributedSynchronizationHandle? TryAcquireAllWriteLocks(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout, bool observeLockLostToken, CancellationToken cancellationToken) =>
        SyncViaAsync.Run(static s => s.provider.TryAcquireAllWriteLocksAsync(s.names, s.timeout, s.observeLockLostToken, s.cancellationToken), (provider, names, timeout, observeLockLostToken, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.AcquireWriteLock(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each reader-writer lock.</param>
    /// <param name="names">The names of the reader-writer locks on which to acquire write locks.</param>
    /// <param name="timeout">The total time to wait for all write locks to be acquired.</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static IDistributedSynchronizationHandle AcquireAllWriteLocks(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(static s => s.provider.AcquireAllWriteLocksAsync(s.names, s.timeout, s.cancellationToken), (provider, names, timeout, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.AcquireWriteLock(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each reader-writer lock.</param>
    /// <param name="names">The names of the reader-writer locks on which to acquire write locks.</param>
    /// <param name="timeout">The total time to wait for all write locks to be acquired.</param>
    /// <param name="observeLockLostToken">
    /// Whether to observe the <see cref="IDistributedSynchronizationHandle.HandleLostToken"/> of already-acquired handles while acquiring the remaining ones.
    /// If a handle is lost mid-acquisition, all acquired handles are released and a <see cref="DistributedLockLostException"/> is thrown.
    /// Has no effect when only one name is given. Note that accessing the token may start background monitoring for some providers.
    /// </param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static IDistributedSynchronizationHandle AcquireAllWriteLocks(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout, bool observeLockLostToken, CancellationToken cancellationToken) =>
        SyncViaAsync.Run(static s => s.provider.AcquireAllWriteLocksAsync(s.names, s.timeout, s.observeLockLostToken, s.cancellationToken), (provider, names, timeout, observeLockLostToken, cancellationToken));

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.TryAcquireWriteLockAsync(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each reader-writer lock.</param>
    /// <param name="names">The names of the reader-writer locks on which to acquire write locks.</param>
    /// <param name="timeout">The total time to wait for all write locks to be acquired.</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static ValueTask<IDistributedSynchronizationHandle?> TryAcquireAllWriteLocksAsync(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        provider.TryAcquireAllWriteLocksInternalAsync(names, timeout, observeLockLostToken: false, cancellationToken).GetHandleOrDefault();

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.TryAcquireWriteLockAsync(TimeSpan, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each reader-writer lock.</param>
    /// <param name="names">The names of the reader-writer locks on which to acquire write locks.</param>
    /// <param name="timeout">The total time to wait for all write locks to be acquired.</param>
    /// <param name="observeLockLostToken">
    /// Whether to observe the <see cref="IDistributedSynchronizationHandle.HandleLostToken"/> of already-acquired handles while acquiring the remaining ones.
    /// If a handle is lost mid-acquisition, all acquired handles are released and null is returned.
    /// Has no effect when only one name is given. Note that accessing the token may start background monitoring for some providers.
    /// </param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static ValueTask<IDistributedSynchronizationHandle?> TryAcquireAllWriteLocksAsync(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan timeout, bool observeLockLostToken, CancellationToken cancellationToken) =>
        provider.TryAcquireAllWriteLocksInternalAsync(names, timeout, observeLockLostToken, cancellationToken).GetHandleOrDefault();

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.AcquireWriteLockAsync(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each reader-writer lock.</param>
    /// <param name="names">The names of the reader-writer locks on which to acquire write locks.</param>
    /// <param name="timeout">The total time to wait for all write locks to be acquired.</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireAllWriteLocksAsync(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        provider.TryAcquireAllWriteLocksInternalAsync(names, timeout, observeLockLostToken: false, cancellationToken).GetHandleOrTimeout();

    /// <summary>
    /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> for each name in <paramref name="names" /> and then
    /// <see cref="IDistributedReaderWriterLock.AcquireWriteLockAsync(TimeSpan?, CancellationToken)" /> on each created instance, combining the results into a composite handle.
    /// </summary>
    /// <param name="provider">The provider used to create each reader-writer lock.</param>
    /// <param name="names">The names of the reader-writer locks on which to acquire write locks.</param>
    /// <param name="timeout">The total time to wait for all write locks to be acquired.</param>
    /// <param name="observeLockLostToken">
    /// Whether to observe the <see cref="IDistributedSynchronizationHandle.HandleLostToken"/> of already-acquired handles while acquiring the remaining ones.
    /// If a handle is lost mid-acquisition, all acquired handles are released and a <see cref="DistributedLockLostException"/> is thrown.
    /// Has no effect when only one name is given. Note that accessing the token may start background monitoring for some providers.
    /// </param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
    public static ValueTask<IDistributedSynchronizationHandle> AcquireAllWriteLocksAsync(this IDistributedReaderWriterLockProvider provider, IReadOnlyList<string> names, TimeSpan? timeout, bool observeLockLostToken, CancellationToken cancellationToken) =>
        provider.TryAcquireAllWriteLocksInternalAsync(names, timeout, observeLockLostToken, cancellationToken).GetHandleOrTimeout();

    # endregion
}