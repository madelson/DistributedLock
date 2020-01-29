using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql.ConnectionMultiplexing
{
    /// <summary>
    /// Allows multiple SQL application locks to be taken on a single connection.
    /// 
    /// This class is thread-safe except for <see cref="IDisposable.Dispose"/>
    /// </summary>
    internal sealed class MultiplexedConnectionLock : IDisposable
    {
        /// <summary>
        /// Protects access to <see cref="outstandingHandles"/> and <see cref="connection"/>. We use
        /// <see cref="SemaphoreSlim"/> over a normal lock because of its async support
        /// </summary>
        private readonly SemaphoreSlim mutex = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        private readonly Dictionary<string, HandleReference> outstandingHandles = new Dictionary<string, HandleReference>();
        private readonly DbConnection connection;

        public MultiplexedConnectionLock(string connectionString)
        {
            this.connection = SqlHelpers.CreateConnection(connectionString);
        }

        public Result TryAcquire<TLockCookie>(
            string lockName,
            int timeoutMillis,
            ISqlSynchronizationStrategy<TLockCookie> strategy,
            bool opportunistic)
            where TLockCookie : class
        {
            if (!this.mutex.Wait(opportunistic ? TimeSpan.Zero : Timeout.InfiniteTimeSpan))
            {
                // mutex wasn't free, so just give up
                return this.GetFailureResultNoLock(Reason.MutexTimeout, opportunistic, timeoutMillis);
            }
            try
            {
                if (this.outstandingHandles.ContainsKey(lockName))
                {
                    // we won't try to hold the same lock twice on one connection. At some point, we could
                    // support this case in-memory using a counter for each multiply-held lock name and being careful
                    // with modes
                    return this.GetFailureResultNoLock(Reason.AlreadyHeld, opportunistic, timeoutMillis);
                }

                if (this.connection.State != ConnectionState.Open) { this.connection.Open(); }

                var lockCookie = strategy.TryAcquire(this.connection, lockName, opportunistic ? 0 : timeoutMillis);
                if (lockCookie != null)
                {
                    return this.CreateSuccessResult(strategy, lockName, lockCookie);
                }

                return this.GetFailureResultNoLock(Reason.AcquireTimeout, opportunistic, timeoutMillis);
            }
            finally
            {
                this.CloseConnectionIfNeededNoLock();
                this.mutex.Release();
            }
        }

        public async Task<Result> TryAcquireAsync<TLockCookie>(
            string lockName,
            int timeoutMillis,
            ISqlSynchronizationStrategy<TLockCookie> strategy,
            CancellationToken cancellationToken,
            bool opportunistic)
            where TLockCookie : class
        {
            if (!await this.mutex.WaitAsync(opportunistic ? TimeSpan.Zero : Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false))
            {
                // mutex wasn't free, so just give up
                return this.GetFailureResultNoLock(Reason.MutexTimeout, opportunistic, timeoutMillis);
            }
            try
            {
                if (this.outstandingHandles.ContainsKey(lockName))
                {
                    // we won't try to hold the same lock twice on one connection. At some point, we could
                    // support this case in-memory using a counter for each multiply-held lock name and being careful
                    // with modes
                    return this.GetFailureResultNoLock(Reason.AlreadyHeld, opportunistic, timeoutMillis);
                }

                if (this.connection.State != ConnectionState.Open)
                {
                    await this.connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                }

                var lockCookie = await strategy.TryAcquireAsync(this.connection, lockName, opportunistic ? 0 : timeoutMillis, cancellationToken).ConfigureAwait(false);
                if (lockCookie != null)
                {
                    return this.CreateSuccessResult(strategy, lockName, lockCookie);
                }

                // we failed to acquire the lock, so we should retry if we were being opportunistic and artificially
                // shortened the timeout
                return this.GetFailureResultNoLock(Reason.AcquireTimeout, opportunistic, timeoutMillis);
            }
            finally
            {
                this.CloseConnectionIfNeededNoLock();
                this.mutex.Release();
            }
        }

        private Result CreateSuccessResult<TLockCookie>(ISqlSynchronizationStrategy<TLockCookie> strategy, string lockName, TLockCookie lockCookie)
            where TLockCookie : class
        {
            var nonThreadSafeHandle = new ReleaseAction(() => this.ReleaseNoLock(strategy, lockName, lockCookie));
            var threadSafeHandle = new ThreadSafeReleaseAction(this.mutex, nonThreadSafeHandle);
            this.outstandingHandles.Add(lockName, new HandleReference(threadSafeHandle: threadSafeHandle, nonThreadSafeHandle: nonThreadSafeHandle));
            return new Result(threadSafeHandle);
        }

        public async Task<bool> CleanupAsync()
        {
            await this.mutex.WaitAsync().ConfigureAwait(false);
            try
            {
                List<string>? toRemove = null;
                foreach (var kvp in this.outstandingHandles)
                {
                    if (!kvp.Value.ThreadSafeHandle.TryGetTarget(out var ignored))
                    {
                        (toRemove ??= new List<string>()).Add(kvp.Key);
                    }
                }
                
                if (toRemove != null)
                {
                    foreach (var lockName in toRemove)
                    {
                        try { this.outstandingHandles[lockName].NonThreadSafeHandle.Dispose(); }
                        catch
                        {
                            // suppress exceptions. If this fails there's not much else we can do
                        }
                    }
                }
                
                return this.outstandingHandles.Count > 0;
            }
            finally
            {
                this.mutex.Release();
            }
        }

        public void Dispose()
        {
            if (this.outstandingHandles.Count != 0) { throw new InvalidOperationException("unsafe dispose"); }

            this.connection.Dispose();
        }

        private enum Reason
        {
            MutexTimeout,
            AlreadyHeld,
            AcquireTimeout,
        }

        private Result GetFailureResultNoLock(Reason reason, bool opportunistic, int timeoutMillis)
        {
            // only opportunistic acquisitions trigger retries
            if (!opportunistic) { return new Result(MultiplexedConnectionLockRetry.NoRetry); }

            switch (reason)
            {
                case Reason.MutexTimeout:
                case Reason.AlreadyHeld:
                    // in these cases, the current lock is busy so we allow retry but on
                    // a different lock instance
                    return new Result(MultiplexedConnectionLockRetry.Retry);
                case Reason.AcquireTimeout:
                    return new Result(
                        // if acquire timed out and the caller requested a zero timeout, that's conventional failure
                        // and we shouldn't retry
                        timeoutMillis == 0 ? MultiplexedConnectionLockRetry.NoRetry
                            // if we're not holding anything, then it's safe to retry on this instance since we can't
                            // possibly block a release
                            : this.outstandingHandles.Count == 0 ? MultiplexedConnectionLockRetry.RetryOnThisLock
                            // otherwise, retry on a different instance
                            : MultiplexedConnectionLockRetry.Retry
                        );
                default:
                    throw new InvalidOperationException("unexpected reason");
            }
        }

        private void ReleaseNoLock<TLockCookie>(ISqlSynchronizationStrategy<TLockCookie> strategy, string lockName, TLockCookie lockCookie)
            where TLockCookie : class
        {
            try
            {
                strategy.Release(this.connection, lockName, lockCookie);
            }
            finally
            {
                this.outstandingHandles.Remove(lockName);
                this.CloseConnectionIfNeededNoLock();
            }
        }

        private void CloseConnectionIfNeededNoLock()
        {
            if (this.outstandingHandles.Count == 0 && this.connection.State == ConnectionState.Open)
            {
                this.connection.Close();
            }
        }

        public struct Result
        {
            public Result(IDisposable handle)
            {
                this.Handle = handle;
                this.Retry = MultiplexedConnectionLockRetry.NoRetry;
            }

            public Result(MultiplexedConnectionLockRetry retry)
            {
                this.Handle = null;
                this.Retry = retry;
            }

            public IDisposable? Handle { get; }
            public MultiplexedConnectionLockRetry Retry { get; }
        }

        /// <summary>
        /// To ensure cleanup, we store two handle variants. A thread-safe version is returned to the caller while we hold
        /// a strong reference to the underlying non-thread-safe cleanup handle. If the returned handle is GC'd without releasing
        /// (abandoned) then the cleanup thread can use the cleanup handle as a back-up
        /// </summary>
        private struct HandleReference
        {
            public HandleReference(ThreadSafeReleaseAction threadSafeHandle, ReleaseAction nonThreadSafeHandle)
            {
                this.ThreadSafeHandle = new WeakReference<ThreadSafeReleaseAction>(threadSafeHandle);
                this.NonThreadSafeHandle = nonThreadSafeHandle;
            }

            public WeakReference<ThreadSafeReleaseAction> ThreadSafeHandle { get; }
            public ReleaseAction NonThreadSafeHandle { get; }
        }

        private sealed class ThreadSafeReleaseAction : IDisposable
        {
            private SemaphoreSlim? mutex;
            private IDisposable? cleanupHandle;

            public ThreadSafeReleaseAction(SemaphoreSlim mutex, IDisposable cleanupHandle)
            {
                this.mutex = mutex;
                this.cleanupHandle = cleanupHandle;
            }

            void IDisposable.Dispose()
            {
                var mutex = Interlocked.Exchange(ref this.mutex, null);
                if (mutex != null)
                {
                    mutex.Wait();
                    try { this.cleanupHandle!.Dispose(); }
                    finally { mutex.Release(); }

                    this.cleanupHandle = null;
                }
            }
        }
    }

    internal enum MultiplexedConnectionLockRetry
    {
        NoRetry,
        RetryOnThisLock,
        Retry,
    }
}
