// AUTO-GENERATED
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    /// <summary>
    /// Productivity helper methods for <see cref="IDistributedLockProvider" />
    /// </summary>
    public static class DistributedLockProviderExtensions
    {
        /// <summary>
        /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> and then
        /// <see cref="IDistributedLock.TryAcquire(TimeSpan, CancellationToken)" />.
        /// </summary>
        public static IDistributedLockHandle? TryAcquireLock(this IDistributedLockProvider provider, string name, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            (provider ?? throw new ArgumentNullException(nameof(provider))).CreateLock(name).TryAcquire(timeout, cancellationToken);

        /// <summary>
        /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> and then
        /// <see cref="IDistributedLock.Acquire(TimeSpan?, CancellationToken)" />.
        /// </summary>
        public static IDistributedLockHandle AcquireLock(this IDistributedLockProvider provider, string name, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            (provider ?? throw new ArgumentNullException(nameof(provider))).CreateLock(name).Acquire(timeout, cancellationToken);

        /// <summary>
        /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> and then
        /// <see cref="IDistributedLock.TryAcquireAsync(TimeSpan, CancellationToken)" />.
        /// </summary>
        public static ValueTask<IDistributedLockHandle?> TryAcquireLockAsync(this IDistributedLockProvider provider, string name, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            (provider ?? throw new ArgumentNullException(nameof(provider))).CreateLock(name).TryAcquireAsync(timeout, cancellationToken);

        /// <summary>
        /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string)" /> and then
        /// <see cref="IDistributedLock.AcquireAsync(TimeSpan?, CancellationToken)" />.
        /// </summary>
        public static ValueTask<IDistributedLockHandle> AcquireLockAsync(this IDistributedLockProvider provider, string name, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            (provider ?? throw new ArgumentNullException(nameof(provider))).CreateLock(name).AcquireAsync(timeout, cancellationToken);
    }
}