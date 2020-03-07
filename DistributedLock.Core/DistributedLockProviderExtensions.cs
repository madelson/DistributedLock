using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    /// <summary>
    /// Productivity helper methods for <see cref="IDistributedLockProvider"/>
    /// </summary>
    public static class DistributedLockProviderExtensions
    {
        // todo review hiding of these methods vs. implementation-specific APIs

        /// <summary>
        /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string, bool)"/> and then
        /// <see cref="IDistributedLock.TryAcquire(TimeSpan, CancellationToken)"/>
        /// </summary>
        public static IDistributedLockHandle? TryAcquire(
            this IDistributedLockProvider provider,
            string name,
            TimeSpan timeout = default,
            CancellationToken cancellationToken = default,
            bool exactName = false) =>
            CreateLock(provider, name, exactName).TryAcquire(timeout, cancellationToken);

        /// <summary>
        /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string, bool)"/> and then
        /// <see cref="IDistributedLock.Acquire(TimeSpan?, CancellationToken)"/>
        /// </summary>
        public static IDistributedLockHandle Acquire(
            this IDistributedLockProvider provider,
            string name,
            TimeSpan? timeout = default,
            CancellationToken cancellationToken = default,
            bool exactName = false) =>
            CreateLock(provider, name, exactName).Acquire(timeout, cancellationToken);

        /// <summary>
        /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string, bool)"/> and then
        /// <see cref="IDistributedLock.TryAcquireAsync(TimeSpan, CancellationToken)"/>
        /// </summary>
        public static ValueTask<IDistributedLockHandle?> TryAcquireAsync(
            this IDistributedLockProvider provider,
            string name,
            TimeSpan timeout = default,
            CancellationToken cancellationToken = default,
            bool exactName = false) =>
            CreateLock(provider, name, exactName).TryAcquireAsync(timeout, cancellationToken);

        /// <summary>
        /// Equivalent to calling <see cref="IDistributedLockProvider.CreateLock(string, bool)"/> and then
        /// <see cref="IDistributedLock.AcquireAsync(TimeSpan?, CancellationToken)"/>
        /// </summary>
        public static ValueTask<IDistributedLockHandle> AcquireAsync(
            this IDistributedLockProvider provider,
            string name,
            TimeSpan? timeout = default,
            CancellationToken cancellationToken = default,
            bool exactName = false) =>
            CreateLock(provider, name, exactName).AcquireAsync(timeout, cancellationToken);

        private static IDistributedLock CreateLock(IDistributedLockProvider provider, string name, bool exactName) =>
            (provider ?? throw new ArgumentNullException(nameof(provider))).CreateLock(name, exactName);
    }
}
