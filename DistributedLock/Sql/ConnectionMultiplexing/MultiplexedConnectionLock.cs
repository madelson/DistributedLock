using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
        private readonly Dictionary<string, WeakReference<Handle>> outstandingHandles = new Dictionary<string, WeakReference<Handle>>();
        private readonly SqlConnection connection;

        public MultiplexedConnectionLock(string connectionString)
        {
            this.connection = new SqlConnection(connectionString);
        }

        public Result TryAcquire(
            string lockName,
            int timeoutMillis,
            SqlApplicationLock.Mode mode,
            bool opportunistic)
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

                if (SqlApplicationLock.ExecuteAcquireCommand(this.connection, lockName, opportunistic ? 0 : timeoutMillis, mode))
                {
                    var handle = new Handle(this, lockName);
                    this.outstandingHandles.Add(lockName, new WeakReference<Handle>(handle));
                    return new Result(handle);
                }

                return this.GetFailureResultNoLock(Reason.AcquireTimeout, opportunistic, timeoutMillis);
            }
            finally
            {
                this.CloseConnectionIfNeededNoLock();
                this.mutex.Release();
            }
        }

        public async Task<Result> TryAcquireAsync(
            string lockName,
            int timeoutMillis,
            SqlApplicationLock.Mode mode,
            CancellationToken cancellationToken,
            bool opportunistic)
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

                if (await SqlApplicationLock.ExecuteAcquireCommandAsync(this.connection, lockName, opportunistic ? 0 : timeoutMillis, mode, cancellationToken).ConfigureAwait(false))
                {
                    var handle = new Handle(this, lockName);
                    this.outstandingHandles.Add(lockName, new WeakReference<Handle>(handle));
                    return new Result(handle);
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

        public async Task<bool> CleanupAsync()
        {
            await this.mutex.WaitAsync().ConfigureAwait(false);
            try
            {
                List<string> toRemove = null;
                foreach (var kvp in this.outstandingHandles)
                {
                    Handle ignored;
                    if (!kvp.Value.TryGetTarget(out ignored))
                    {
                        (toRemove ?? (toRemove = new List<string>())).Add(kvp.Key);
                    }
                }
                
                if (toRemove != null)
                {
                    foreach (var lockName in toRemove)
                    {
                        try { await SqlApplicationLock.ExecuteReleaseCommandAsync(this.connection, lockName).ConfigureAwait(false); }
                        catch
                        {
                            // suppress exceptions. If this fails there's not much else we can do
                        }
                        this.outstandingHandles.Remove(lockName);
                    }
                }
                
                return this.outstandingHandles.Count > 0;
            }
            finally
            {
                this.CloseConnectionIfNeededNoLock();
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

        private void ReleaseNoLock(string lockName)
        {
            try
            {
                SqlApplicationLock.ExecuteReleaseCommand(this.connection, lockName);
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

            public IDisposable Handle { get; }
            public MultiplexedConnectionLockRetry Retry { get; }
        }

        private sealed class Handle : IDisposable
        {
            private MultiplexedConnectionLock @lock;
            private readonly string lockName;

            public Handle(MultiplexedConnectionLock @lock, string lockName)
            {
                this.@lock = @lock;
                this.lockName = lockName;
            }

            void IDisposable.Dispose()
            {
                var @lock = Interlocked.Exchange(ref this.@lock, null);
                if (@lock != null)
                {
                    @lock.mutex.Wait();
                    try { @lock.ReleaseNoLock(this.lockName); }
                    finally { @lock.mutex.Release(); }
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
