using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal
{
    // todo revisit whether these should be copied or generated files vs internal APIs

#if DEBUG
    public
#else
    internal
#endif
    static class DistributedLockHelpers
    {
        public static string ToSafeLockName(string name, int maxNameLength, Func<string, string> convertToValidName)
        {
            if (name == null) { throw new ArgumentNullException(nameof(name)); }

            var validBaseLockName = convertToValidName(name);
            if (validBaseLockName == name && validBaseLockName.Length <= maxNameLength)
            {
                return name;
            }

            using var sha = SHA512.Create();
            var hash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(name)));

            if (hash.Length >= maxNameLength)
            {
                return hash.Substring(0, length: maxNameLength);
            }

            var prefix = validBaseLockName.Substring(0, Math.Min(validBaseLockName.Length, maxNameLength - hash.Length));
            return prefix + hash;
        }

        #region ---- IInternalDistributedLock implementations ----
        public static ValueTask<THandle> AcquireAsync<THandle>(IInternalDistributedLock<THandle> @lock, TimeSpan? timeout, CancellationToken cancellationToken)
            where THandle : class, IDistributedLockHandle =>
            @lock.InternalTryAcquireAsync(timeout, cancellationToken).ThrowTimeoutIfNull();

        public static THandle Acquire<THandle>(IInternalDistributedLock<THandle> @lock, TimeSpan? timeout, CancellationToken cancellationToken)
            where THandle : class, IDistributedLockHandle =>
            SyncOverAsync.Run(
                state => AcquireAsync(state.@lock, state.timeout, state.cancellationToken),
                (@lock, timeout, cancellationToken),
                willGoAsync: @lock.WillGoAsync(timeout, cancellationToken)
            );

        public static THandle? TryAcquire<THandle>(IInternalDistributedLock<THandle> @lock, TimeSpan timeout, CancellationToken cancellationToken)
            where THandle : class, IDistributedLockHandle =>
            SyncOverAsync.Run(
                state => state.@lock.InternalTryAcquireAsync(state.timeout, state.cancellationToken),
                (@lock, timeout, cancellationToken),
                willGoAsync: @lock.WillGoAsync(timeout, cancellationToken)
            );
        #endregion

        #region ---- IInternalDistributedReaderWriterLock implementations ----
        public static ValueTask<THandle> AcquireAsync<THandle>(IInternalDistributedReaderWriterLock<THandle> @lock, TimeSpan? timeout, CancellationToken cancellationToken, bool isWrite)
            where THandle : class, IDistributedLockHandle =>
            @lock.InternalTryAcquireAsync(timeout, cancellationToken, isWrite).ThrowTimeoutIfNull();

        public static THandle Acquire<THandle>(IInternalDistributedReaderWriterLock<THandle> @lock, TimeSpan? timeout, CancellationToken cancellationToken, bool isWrite)
            where THandle : class, IDistributedLockHandle =>
            SyncOverAsync.Run(
                state => AcquireAsync(state.@lock, state.timeout, state.cancellationToken, state.isWrite),
                (@lock, timeout, cancellationToken, isWrite),
                willGoAsync: false
            );

        public static THandle? TryAcquire<THandle>(IInternalDistributedReaderWriterLock<THandle> @lock, TimeSpan timeout, CancellationToken cancellationToken, bool isWrite)
            where THandle : class, IDistributedLockHandle =>
            SyncOverAsync.Run(
                state => state.@lock.InternalTryAcquireAsync(state.timeout, state.cancellationToken, state.isWrite),
                (@lock, timeout, cancellationToken, isWrite),
                willGoAsync: false
            );
        #endregion

        #region ---- IInternalDistributedUpgradeableReaderWriterLock implementations ----
        public static ValueTask<TUpgradeableHandle> AcquireUpgradeableReadLockAsync<THandle, TUpgradeableHandle>(IInternalDistributedUpgradeableReaderWriterLock<THandle, TUpgradeableHandle> @lock, TimeSpan? timeout, CancellationToken cancellationToken)
            where THandle : class, IDistributedLockHandle
            where TUpgradeableHandle : class, IDistributedLockUpgradeableHandle =>
            @lock.InternalTryAcquireUpgradeableReadLockAsync(timeout, cancellationToken).ThrowTimeoutIfNull();

        public static TUpgradeableHandle AcquireUpgradeableReadLock<THandle, TUpgradeableHandle>(IInternalDistributedUpgradeableReaderWriterLock<THandle, TUpgradeableHandle> @lock, TimeSpan? timeout, CancellationToken cancellationToken)
            where THandle : class, IDistributedLockHandle
            where TUpgradeableHandle : class, IDistributedLockUpgradeableHandle =>
            SyncOverAsync.Run(
                state => AcquireUpgradeableReadLockAsync(state.@lock, state.timeout, state.cancellationToken),
                (@lock, timeout, cancellationToken),
                willGoAsync: false
            );

        public static TUpgradeableHandle? TryAcquireUpgradeableReadLock<THandle, TUpgradeableHandle>(IInternalDistributedUpgradeableReaderWriterLock<THandle, TUpgradeableHandle> @lock, TimeSpan timeout, CancellationToken cancellationToken)
            where THandle : class, IDistributedLockHandle
            where TUpgradeableHandle : class, IDistributedLockUpgradeableHandle =>
            SyncOverAsync.Run(
                state => state.@lock.InternalTryAcquireUpgradeableReadLockAsync(state.timeout, state.cancellationToken),
                (@lock, timeout, cancellationToken),
                willGoAsync: false
            );
        #endregion

        #region ---- IDistributedLockUpgradeableHandle implementations ----
        public static ValueTask UpgradeToWriteLockAsync(IInternalDistributedLockUpgradeableHandle handle, TimeSpan? timeout, CancellationToken cancellationToken) =>
           handle.InternalTryUpgradeToWriteLockAsync(timeout, cancellationToken).ThrowTimeoutIfFalse();

        public static void UpgradeToWriteLock(IDistributedLockUpgradeableHandle handle, TimeSpan? timeout, CancellationToken cancellationToken) =>
            SyncOverAsync.Run(t => t.handle.UpgradeToWriteLockAsync(t.timeout, t.cancellationToken), (handle, timeout, cancellationToken), false);

        public static bool TryUpgradeToWriteLock(IDistributedLockUpgradeableHandle handle, TimeSpan timeout, CancellationToken cancellationToken) =>
            SyncOverAsync.Run(t => t.handle.TryUpgradeToWriteLockAsync(t.timeout, t.cancellationToken), (handle, timeout, cancellationToken), false);
        #endregion

        private static Exception LockTimeout() => new TimeoutException("Timeout exceeded when trying to acquire the lock");

        public static async ValueTask<T> ThrowTimeoutIfNull<T>(this ValueTask<T?> task) where T : class =>
            await task.ConfigureAwait(false) ?? throw LockTimeout();

        private static async ValueTask ThrowTimeoutIfFalse(this ValueTask<bool> task)
        {
            if (!await task.ConfigureAwait(false)) { throw LockTimeout(); }
        }
    }
}
