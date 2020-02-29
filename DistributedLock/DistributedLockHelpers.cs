using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
#if DEBUG
    public
#else
    internal
#endif
        static class DistributedLockHelpers
    {
        public static int ToInt32Timeout(this TimeSpan timeout, string? paramName = null)
        {
            // based on http://referencesource.microsoft.com/#mscorlib/system/threading/Tasks/Task.cs,959427ac16fa52fa

            var totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(paramName ?? "timeout");
            }

            return (int)totalMilliseconds;
        }

        public static Task<IDisposable> AcquireAsync(IDistributedLockOld @lock, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            var tryAcquireTask = @lock.TryAcquireAsync(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken);
            return ValidateTryAcquireResultAsync(tryAcquireTask, timeout);
        }

        public static IDisposable Acquire(IDistributedLockOld @lock, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            return @lock.TryAcquire(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken)
                ?? throw CreateTryAcquireFailedException(timeout);
        }

        public static async Task<THandle> ValidateTryAcquireResultAsync<THandle>(Task<THandle?> tryAcquireTask, TimeSpan? timeout)
            where THandle : class, IDisposable
        {
            return await tryAcquireTask.ConfigureAwait(false) ?? throw CreateTryAcquireFailedException(timeout);
        }

        public static Exception CreateTryAcquireFailedException(TimeSpan? timeout) =>
            timeout.HasValue && timeout >= TimeSpan.Zero
                ? new TimeoutException("Timeout exceeded when trying to acquire the lock")
                // should never get here
                : (Exception)new InvalidOperationException("Failed to acquire the lock");

        public static IDisposable? TryAcquireWithAsyncCancellation(IDistributedLockOld @lock, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return @lock.TryAcquireAsync(timeout, cancellationToken).GetAwaiter().GetResult();
        }

        public static string ToSafeLockName(string baseLockName, int maxNameLength, Func<string, string> convertToValidName)
        {
            if (baseLockName == null)
                throw new ArgumentNullException("baseLockName");

            var validBaseLockName = convertToValidName(baseLockName);
            if (validBaseLockName == baseLockName && validBaseLockName.Length <= maxNameLength)
            {
                return baseLockName;
            }

            using var sha = SHA512.Create();
            var hash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(baseLockName)));

            if (hash.Length >= maxNameLength)
            {
                return hash.Substring(0, length: maxNameLength);
            }

            var prefix = validBaseLockName.Substring(0, Math.Min(validBaseLockName.Length, maxNameLength - hash.Length));
            return prefix + hash;
        }
    }
}
