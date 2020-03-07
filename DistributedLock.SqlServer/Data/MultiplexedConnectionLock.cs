using Medallion.Threading.Internal;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Data
{
    /// <summary>
    /// Allows multiple SQL application locks to be taken on a single connection.
    /// 
    /// This class is thread-safe except for <see cref="IAsyncDisposable.DisposeAsync"/>
    /// </summary>
    internal sealed class MultiplexedConnectionLock : IAsyncDisposable
    {
        /// <summary>
        /// Protects access to <see cref="_outstandingHandles"/> and <see cref="_connection"/>. We use
        /// <see cref="SemaphoreSlim"/> over a normal lock because of its async support
        /// </summary>
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        private readonly Dictionary<string, HandleReference> _outstandingHandles = new Dictionary<string, HandleReference>();
        private readonly DbConnection _connection;

        public MultiplexedConnectionLock(string connectionString)
        {
            this._connection = new SqlConnection(connectionString);
        }

        public async ValueTask<Result> TryAcquireAsync<TLockCookie>(
            string lockName,
            TimeoutValue timeout,
            ISqlSynchronizationStrategy<TLockCookie> strategy,
            CancellationToken cancellationToken,
            bool opportunistic)
            where TLockCookie : class
        {
            if (!await SemaphoreSlimHelper.WaitAsync(this._mutex, opportunistic ? TimeSpan.Zero : Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false))
            {
                // mutex wasn't free, so just give up
                return this.GetFailureResultNoLock(Reason.MutexTimeout, opportunistic, timeout);
            }
            try
            {
                if (this._outstandingHandles.ContainsKey(lockName))
                {
                    // we won't try to hold the same lock twice on one connection. At some point, we could
                    // support this case in-memory using a counter for each multiply-held lock name and being careful
                    // with modes
                    return this.GetFailureResultNoLock(Reason.AlreadyHeld, opportunistic, timeout);
                }

                if (this._connection.State != ConnectionState.Open)
                {
                    await SqlHelpers.OpenAsync(this._connection, cancellationToken).ConfigureAwait(false);
                }

                var lockCookie = await strategy.TryAcquireAsync(this._connection, lockName, opportunistic ? TimeSpan.Zero : timeout, cancellationToken).ConfigureAwait(false);
                if (lockCookie != null)
                {
                    return this.CreateSuccessResult(strategy, lockName, lockCookie);
                }

                // we failed to acquire the lock, so we should retry if we were being opportunistic and artificially
                // shortened the timeout
                return this.GetFailureResultNoLock(Reason.AcquireTimeout, opportunistic, timeout);
            }
            finally
            {
                try { await this.CloseConnectionIfNeededNoLockAsync().ConfigureAwait(false); }
                finally { this._mutex.Release(); }
            }
        }

        private Result CreateSuccessResult<TLockCookie>(ISqlSynchronizationStrategy<TLockCookie> strategy, string lockName, TLockCookie lockCookie)
            where TLockCookie : class
        {
            var nonThreadSafeHandle = new AsyncReleaseAction(() => this.ReleaseNoLockAsync(strategy, lockName, lockCookie));
            var threadSafeHandle = new Handle(this._mutex, nonThreadSafeHandle);
            this._outstandingHandles.Add(lockName, new HandleReference(threadSafeHandle: threadSafeHandle, nonThreadSafeHandle: nonThreadSafeHandle));
            return new Result(threadSafeHandle);
        }

        public async Task<bool> CleanUpAsync()
        {
            await SemaphoreSlimHelper.WaitAsync(this._mutex, Timeout.InfiniteTimeSpan, CancellationToken.None).ConfigureAwait(false);
            try
            {
                List<string>? toRemove = null;
                foreach (var kvp in this._outstandingHandles)
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
                        try { await this._outstandingHandles[lockName].NonThreadSafeHandle.DisposeAsync().ConfigureAwait(false); }
                        catch
                        {
                            // suppress exceptions. If this fails there's not much else we can do
                        }
                    }
                }
                
                return this._outstandingHandles.Count > 0;
            }
            finally
            {
                this._mutex.Release();
            }
        }

        public ValueTask DisposeAsync()
        {
            if (this._outstandingHandles.Count != 0) { throw new InvalidOperationException("unsafe dispose"); }

            return SqlHelpers.DisposeAsync(this._connection);
        }

        private enum Reason
        {
            MutexTimeout,
            AlreadyHeld,
            AcquireTimeout,
        }

        private Result GetFailureResultNoLock(Reason reason, bool opportunistic, TimeoutValue timeout)
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
                        timeout.IsZero ? MultiplexedConnectionLockRetry.NoRetry
                            // if we're not holding anything, then it's safe to retry on this instance since we can't
                            // possibly block a release
                            : this._outstandingHandles.Count == 0 ? MultiplexedConnectionLockRetry.RetryOnThisLock
                            // otherwise, retry on a different instance
                            : MultiplexedConnectionLockRetry.Retry
                        );
                default:
                    throw new InvalidOperationException("unexpected reason");
            }
        }

        private async ValueTask ReleaseNoLockAsync<TLockCookie>(ISqlSynchronizationStrategy<TLockCookie> strategy, string lockName, TLockCookie lockCookie)
            where TLockCookie : class
        {
            try
            {
                await strategy.ReleaseAsync(this._connection, lockName, lockCookie).ConfigureAwait(false);
            }
            finally
            {
                this._outstandingHandles.Remove(lockName);
                await this.CloseConnectionIfNeededNoLockAsync().ConfigureAwait(false);
            }
        }

        private ValueTask CloseConnectionIfNeededNoLockAsync()
        {
            return this._outstandingHandles.Count == 0 && this._connection.State == ConnectionState.Open
                ? SqlHelpers.CloseAsync(this._connection)
                : default;
        }

        public struct Result
        {
            public Result(IDistributedLockHandle handle)
            {
                this.Handle = handle;
                this.Retry = MultiplexedConnectionLockRetry.NoRetry;
            }

            public Result(MultiplexedConnectionLockRetry retry)
            {
                this.Handle = null;
                this.Retry = retry;
            }

            public IDistributedLockHandle? Handle { get; }
            public MultiplexedConnectionLockRetry Retry { get; }
        }

        /// <summary>
        /// To ensure cleanup, we store two handle variants. A thread-safe version is returned to the caller while we hold
        /// a strong reference to the underlying non-thread-safe cleanup handle. If the returned handle is GC'd without releasing
        /// (abandoned) then the cleanup thread can use the cleanup handle as a back-up
        /// </summary>
        private struct HandleReference
        {
            public HandleReference(Handle threadSafeHandle, IAsyncDisposable nonThreadSafeHandle)
            {
                this.ThreadSafeHandle = new WeakReference<Handle>(threadSafeHandle);
                this.NonThreadSafeHandle = nonThreadSafeHandle;
            }

            public WeakReference<Handle> ThreadSafeHandle { get; }
            public IAsyncDisposable NonThreadSafeHandle { get; }
        }

        private sealed class AsyncReleaseAction : IAsyncDisposable
        {
            private Func<ValueTask>? _disposeAsync;

            public AsyncReleaseAction(Func<ValueTask> disposeAsync)
            {
                this._disposeAsync = disposeAsync;
            }

            public ValueTask DisposeAsync() => Interlocked.Exchange(ref this._disposeAsync, null)?.Invoke() ?? default;
        }

        private sealed class Handle : IDistributedLockHandle
        {
            private SemaphoreSlim? _mutex;
            private IAsyncDisposable? _cleanupHandle;

            public Handle(SemaphoreSlim mutex, IAsyncDisposable cleanupHandle)
            {
                this._mutex = mutex;
                this._cleanupHandle = cleanupHandle;
            }

            public CancellationToken HandleLostToken => throw new NotImplementedException();

            public async ValueTask DisposeAsync()
            {
                var mutex = Interlocked.Exchange(ref this._mutex, null);
                if (mutex != null)
                {
                    var cleanupHandle = Interlocked.Exchange(ref this._cleanupHandle, null)!;
                    await SemaphoreSlimHelper.WaitAsync(mutex, Timeout.InfiniteTimeSpan, CancellationToken.None).ConfigureAwait(false);
                    try { await cleanupHandle.DisposeAsync().ConfigureAwait(false); }
                    finally { mutex.Release(); }
                }
            }

            void IDisposable.Dispose() => SyncOverAsync.Run(@this => @this.DisposeAsync(), this, willGoAsync: false);
        }
    }

    internal enum MultiplexedConnectionLockRetry
    {
        NoRetry,
        RetryOnThisLock,
        Retry,
    }
}
