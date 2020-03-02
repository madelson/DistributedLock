using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal
{
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

        public static async ValueTask<THandle> AcquireAsync<THandle>(IInternalDistributedLock<THandle> @lock, TimeSpan? timeout, CancellationToken cancellationToken)
            where THandle : class, IDistributedLockHandle =>
            await @lock.InternalTryAcquireAsync(timeout, cancellationToken).ConfigureAwait(false) ?? throw LockTimeout();

        public static THandle Acquire<THandle>(IInternalDistributedLock<THandle> @lock, TimeSpan? timeout, CancellationToken cancellationToken)
            where THandle : class, IDistributedLockHandle =>
            SyncOverAsync.Run(
                ((IInternalDistributedLock<THandle> @lock, TimeSpan? timeout, CancellationToken cancellationToken) state) =>
                    AcquireAsync(state.@lock, state.timeout, state.cancellationToken),
                (@lock, timeout, cancellationToken),
                willGoAsync: @lock.WillGoAsync(timeout, cancellationToken)
            );

        public static THandle? TryAcquire<THandle>(IInternalDistributedLock<THandle> @lock, TimeSpan timeout, CancellationToken cancellationToken)
            where THandle : class, IDistributedLockHandle =>
            SyncOverAsync.Run(
                 ((IInternalDistributedLock<THandle> @lock, TimeSpan timeout, CancellationToken cancellationToken) state) =>
                    state.@lock.TryAcquireAsync(timeout, cancellationToken),
                (@lock, timeout, cancellationToken),
                willGoAsync: @lock.WillGoAsync(timeout, cancellationToken)
            );

        private static Exception LockTimeout() => new TimeoutException("Timeout exceeded when trying to acquire the lock");
    }
}
