using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal.Data
{
    /// <summary>
    /// Allows multiple SQL application locks to be taken on a single connection.
    /// 
    /// This class is thread-safe except for <see cref="IAsyncDisposable.DisposeAsync"/>
    /// </summary>
    internal sealed class MultiplexedConnectionLock : IAsyncDisposable
    {
        /// <summary>
        /// Protects access to <see cref="_heldLocksToKeepaliveCadences"/> and <see cref="_connection"/>
        /// </summary>
        private readonly AsyncLock _mutex = AsyncLock.Create();
        private readonly Dictionary<string, TimeoutValue> _heldLocksToKeepaliveCadences = new Dictionary<string, TimeoutValue>();
        private readonly DatabaseConnection _connection;

        public MultiplexedConnectionLock(DatabaseConnection connection)
        {
            this._connection = connection;
        }

        public async ValueTask<Result> TryAcquireAsync<TLockCookie>(
            string name,
            TimeoutValue timeout,
            IDbSynchronizationStrategy<TLockCookie> strategy,
            TimeoutValue keepaliveCadence,
            CancellationToken cancellationToken,
            bool opportunistic)
            where TLockCookie : class
        {
            using var mutextHandle = await this._mutex.TryAcquireAsync(opportunistic ? TimeSpan.Zero : Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            if (mutextHandle == null)
            {
                // mutex wasn't free, so just give up
                Invariant.Require(opportunistic);
                // The current lock is busy so we allow retry but on a different lock instance. We can't safely dispose
                // since we never acquired the mutex so we can't check _heldLocks
                return new Result(MultiplexedConnectionLockRetry.Retry, canSafelyDispose: false);
            }

            try
            {
                if (this._heldLocksToKeepaliveCadences.ContainsKey(name))
                {
                    // we won't try to hold the same lock twice on one connection. At some point, we could
                    // support this case in-memory using a counter for each multiply-held lock name and being careful
                    // with modes
                    return this.GetFailureResultNoLock(isAlreadyHeld: true, opportunistic, timeout);
                }

                if (!this._connection.CanExecuteQueries)
                {
                    await this._connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                }

                var lockCookie = await strategy.TryAcquireAsync(this._connection, name, opportunistic ? TimeSpan.Zero : timeout, cancellationToken).ConfigureAwait(false);
                if (lockCookie != null)
                {
                    var handle = new Handle<TLockCookie>(this, strategy, name, lockCookie).WithManagedFinalizer();
                    this._heldLocksToKeepaliveCadences.Add(name, keepaliveCadence);
                    if (!keepaliveCadence.IsInfinite) { this.SetKeepaliveCadenceNoLock(); }
                    return new Result(handle);
                }

                // we failed to acquire the lock, so we should retry if we were being opportunistic and artificially
                // shortened the timeout
                return this.GetFailureResultNoLock(isAlreadyHeld: false, opportunistic, timeout);
            }
            finally
            {
                await this.CloseConnectionIfNeededNoLockAsync().ConfigureAwait(false);
            }
        }

        public ValueTask DisposeAsync()
        {
            Invariant.Require(this._heldLocksToKeepaliveCadences.Count == 0);

            return this._connection.DisposeAsync();
        }

        public async ValueTask<bool> GetIsInUseAsync()
        {
            using var mutexHandle = await this._mutex.TryAcquireAsync(TimeSpan.Zero, CancellationToken.None).ConfigureAwait(false);
            return mutexHandle == null || this._heldLocksToKeepaliveCadences.Count != 0;
        }

        private Result GetFailureResultNoLock(bool isAlreadyHeld, bool opportunistic, TimeoutValue timeout)
        {
            // only opportunistic acquisitions trigger retries
            if (!opportunistic) 
            {
                return new Result(MultiplexedConnectionLockRetry.NoRetry, canSafelyDispose: this._heldLocksToKeepaliveCadences.Count == 0); 
            }

            if (isAlreadyHeld)
            {
                // We're already holding the lock so we allow retry but on a different lock instance.
                // We can't safely dispose because we're holding the lock
                return new Result(MultiplexedConnectionLockRetry.Retry, canSafelyDispose: false);
            }

            // if we get here, we failed due to a timeout
            var isHoldingLocks = this._heldLocksToKeepaliveCadences.Count != 0;

            if (timeout.IsZero)
            {
                // if acquire timed out and the caller requested a zero timeout, that's conventional failure
                // and we shouldn't retry
                return new Result(MultiplexedConnectionLockRetry.NoRetry, canSafelyDispose: !isHoldingLocks);
            }

            if (isHoldingLocks)
            {
                // if we're holding other locks, then we should retry on another lock
                return new Result(MultiplexedConnectionLockRetry.Retry, canSafelyDispose: false);
            }

            // If we're not holding anything, then it's safe to retry on this instance since we can't
            // possibly block a release. It's also safe to dispose this lock, but that won't happen since
            // we're going to re-try on it instead
            return new Result(MultiplexedConnectionLockRetry.RetryOnThisLock, canSafelyDispose: true);
        }

        private async ValueTask ReleaseAsync<TLockCookie>(IDbSynchronizationStrategy<TLockCookie> strategy, string name, TLockCookie lockCookie)
            where TLockCookie : class
        {
            using var _ = await this._mutex.AcquireAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                await strategy.ReleaseAsync(this._connection, name, lockCookie).ConfigureAwait(false);
            }
            finally
            {
                if (this._heldLocksToKeepaliveCadences.TryGetValue(name, out var keepaliveCadence))
                {
                    this._heldLocksToKeepaliveCadences.Remove(name);
                    if (!keepaliveCadence.IsInfinite)
                    {
                        // note: we do this even if we're about to close the connection because we'll want 
                        // the correct cadence set when and if we re-open
                        this.SetKeepaliveCadenceNoLock();
                    }
                }
                await this.CloseConnectionIfNeededNoLockAsync().ConfigureAwait(false);
            }
        }

        private ValueTask CloseConnectionIfNeededNoLockAsync()
        {
            return this._heldLocksToKeepaliveCadences.Count == 0 && this._connection.CanExecuteQueries
                ? this._connection.CloseAsync()
                : default;
        }

        private void SetKeepaliveCadenceNoLock()
        {
            TimeoutValue minCadence = Timeout.InfiniteTimeSpan;
            foreach (var kvp in this._heldLocksToKeepaliveCadences)
            {
                if (kvp.Value.CompareTo(minCadence) < 0)
                {
                    minCadence = kvp.Value;
                }
            }
            this._connection.SetKeepaliveCadence(minCadence);
        }

        public readonly struct Result
        {
            public Result(IDistributedSynchronizationHandle handle)
            {
                this.Handle = handle;
                this.Retry = MultiplexedConnectionLockRetry.NoRetry;
                this.CanSafelyDispose = false; // since we have handle
            }

            public Result(MultiplexedConnectionLockRetry retry, bool canSafelyDispose)
            {
                this.Handle = null;
                this.Retry = retry;
                this.CanSafelyDispose = canSafelyDispose;
            }

            public IDistributedSynchronizationHandle? Handle { get; }
            public MultiplexedConnectionLockRetry Retry { get; }
            public bool CanSafelyDispose { get; }
        }

        private sealed class Handle<TLockCookie> : IDistributedSynchronizationHandle
            where TLockCookie : class
        {
            private readonly string _name;
            private RefBox<(MultiplexedConnectionLock @lock, IDbSynchronizationStrategy<TLockCookie> strategy, TLockCookie lockCookie, IDatabaseConnectionMonitoringHandle? monitoringHandle)>? _box;

            public Handle(MultiplexedConnectionLock @lock, IDbSynchronizationStrategy<TLockCookie> strategy, string name, TLockCookie lockCookie)
            {
                this._name = name;
                this._box = RefBox.Create((@lock, strategy, lockCookie, default(IDatabaseConnectionMonitoringHandle)));
            }

            public CancellationToken HandleLostToken
            {
                get
                {
                    var existingBox = Volatile.Read(ref this._box);

                    if (existingBox != null && existingBox.Value.monitoringHandle == null)
                    {
                        var newHandle = existingBox.Value.@lock._connection.ConnectionMonitor.GetMonitoringHandle();
                        var newContents = existingBox.Value;
                        newContents.monitoringHandle = newHandle;
                        var newBox = RefBox.Create(newContents);
                        var newExistingBox = Interlocked.CompareExchange(ref this._box, newBox, comparand: existingBox);
                        if (newExistingBox == existingBox)
                        {
                            return newHandle.ConnectionLostToken;
                        }

                        existingBox = newExistingBox;
                    }

                    if (existingBox == null) { throw this.ObjectDisposed(); }

                    // must exist here since we only clear the box on dispose or update the contents when creating a token
                    return existingBox.Value.monitoringHandle!.ConnectionLostToken;
                }
            }

            public ValueTask DisposeAsync()
            {
                if (RefBox.TryConsume(ref this._box, out var contents))
                {
                    contents.monitoringHandle?.Dispose();
                    return contents.@lock.ReleaseAsync(contents.strategy, this._name, contents.lockCookie);
                }

                return default;
            }

            void IDisposable.Dispose() => SyncOverAsync.Run(@this => @this.DisposeAsync(), this);
        }
    }

    internal enum MultiplexedConnectionLockRetry
    {
        NoRetry,
        RetryOnThisLock,
        Retry,
    }
}
