// AUTO-GENERATED
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    /// <summary>
    /// Productivity helper methods for <see cref="IDistributedReaderWriterLockProvider" />
    /// </summary>
    public static class DistributedReaderWriterLockProviderExtensions
    {
        /// <summary>
        /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> and then
        /// <see cref="IDistributedReaderWriterLock.TryAcquireReadLock(TimeSpan, CancellationToken)" />.
        /// </summary>
        public static IDistributedLockHandle? TryAcquireReadLock(this IDistributedReaderWriterLockProvider provider, string name, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            (provider ?? throw new ArgumentNullException(nameof(provider))).CreateReaderWriterLock(name).TryAcquireReadLock(timeout, cancellationToken);

        /// <summary>
        /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> and then
        /// <see cref="IDistributedReaderWriterLock.AcquireReadLock(TimeSpan?, CancellationToken)" />.
        /// </summary>
        public static IDistributedLockHandle AcquireReadLock(this IDistributedReaderWriterLockProvider provider, string name, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            (provider ?? throw new ArgumentNullException(nameof(provider))).CreateReaderWriterLock(name).AcquireReadLock(timeout, cancellationToken);

        /// <summary>
        /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> and then
        /// <see cref="IDistributedReaderWriterLock.TryAcquireReadLockAsync(TimeSpan, CancellationToken)" />.
        /// </summary>
        public static ValueTask<IDistributedLockHandle?> TryAcquireReadLockAsync(this IDistributedReaderWriterLockProvider provider, string name, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            (provider ?? throw new ArgumentNullException(nameof(provider))).CreateReaderWriterLock(name).TryAcquireReadLockAsync(timeout, cancellationToken);

        /// <summary>
        /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> and then
        /// <see cref="IDistributedReaderWriterLock.AcquireReadLockAsync(TimeSpan?, CancellationToken)" />.
        /// </summary>
        public static ValueTask<IDistributedLockHandle> AcquireReadLockAsync(this IDistributedReaderWriterLockProvider provider, string name, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            (provider ?? throw new ArgumentNullException(nameof(provider))).CreateReaderWriterLock(name).AcquireReadLockAsync(timeout, cancellationToken);

        /// <summary>
        /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> and then
        /// <see cref="IDistributedReaderWriterLock.TryAcquireWriteLock(TimeSpan, CancellationToken)" />.
        /// </summary>
        public static IDistributedLockHandle? TryAcquireWriteLock(this IDistributedReaderWriterLockProvider provider, string name, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            (provider ?? throw new ArgumentNullException(nameof(provider))).CreateReaderWriterLock(name).TryAcquireWriteLock(timeout, cancellationToken);

        /// <summary>
        /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> and then
        /// <see cref="IDistributedReaderWriterLock.AcquireWriteLock(TimeSpan?, CancellationToken)" />.
        /// </summary>
        public static IDistributedLockHandle AcquireWriteLock(this IDistributedReaderWriterLockProvider provider, string name, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            (provider ?? throw new ArgumentNullException(nameof(provider))).CreateReaderWriterLock(name).AcquireWriteLock(timeout, cancellationToken);

        /// <summary>
        /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> and then
        /// <see cref="IDistributedReaderWriterLock.TryAcquireWriteLockAsync(TimeSpan, CancellationToken)" />.
        /// </summary>
        public static ValueTask<IDistributedLockHandle?> TryAcquireWriteLockAsync(this IDistributedReaderWriterLockProvider provider, string name, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            (provider ?? throw new ArgumentNullException(nameof(provider))).CreateReaderWriterLock(name).TryAcquireWriteLockAsync(timeout, cancellationToken);

        /// <summary>
        /// Equivalent to calling <see cref="IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)" /> and then
        /// <see cref="IDistributedReaderWriterLock.AcquireWriteLockAsync(TimeSpan?, CancellationToken)" />.
        /// </summary>
        public static ValueTask<IDistributedLockHandle> AcquireWriteLockAsync(this IDistributedReaderWriterLockProvider provider, string name, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            (provider ?? throw new ArgumentNullException(nameof(provider))).CreateReaderWriterLock(name).AcquireWriteLockAsync(timeout, cancellationToken);
    }
}