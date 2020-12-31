// AUTO-GENERATED
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    /// <summary>
    /// Productivity helper methods for <see cref="IDistributedSemaphoreProvider" />
    /// </summary>
    public static class DistributedSemaphoreProviderExtensions
    {
        /// <summary>
        /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> and then
        /// <see cref="IDistributedSemaphore.TryAcquire(TimeSpan, CancellationToken)" />.
        /// </summary>
        public static IDistributedLockHandle? TryAcquireSemaphore(this IDistributedSemaphoreProvider provider, string name, int maxCount, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            (provider ?? throw new ArgumentNullException(nameof(provider))).CreateSemaphore(name, maxCount).TryAcquire(timeout, cancellationToken);

        /// <summary>
        /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> and then
        /// <see cref="IDistributedSemaphore.Acquire(TimeSpan?, CancellationToken)" />.
        /// </summary>
        public static IDistributedLockHandle AcquireSemaphore(this IDistributedSemaphoreProvider provider, string name, int maxCount, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            (provider ?? throw new ArgumentNullException(nameof(provider))).CreateSemaphore(name, maxCount).Acquire(timeout, cancellationToken);

        /// <summary>
        /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> and then
        /// <see cref="IDistributedSemaphore.TryAcquireAsync(TimeSpan, CancellationToken)" />.
        /// </summary>
        public static ValueTask<IDistributedLockHandle?> TryAcquireSemaphoreAsync(this IDistributedSemaphoreProvider provider, string name, int maxCount, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            (provider ?? throw new ArgumentNullException(nameof(provider))).CreateSemaphore(name, maxCount).TryAcquireAsync(timeout, cancellationToken);

        /// <summary>
        /// Equivalent to calling <see cref="IDistributedSemaphoreProvider.CreateSemaphore(string, int)" /> and then
        /// <see cref="IDistributedSemaphore.AcquireAsync(TimeSpan?, CancellationToken)" />.
        /// </summary>
        public static ValueTask<IDistributedLockHandle> AcquireSemaphoreAsync(this IDistributedSemaphoreProvider provider, string name, int maxCount, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            (provider ?? throw new ArgumentNullException(nameof(provider))).CreateSemaphore(name, maxCount).AcquireAsync(timeout, cancellationToken);
    }
}