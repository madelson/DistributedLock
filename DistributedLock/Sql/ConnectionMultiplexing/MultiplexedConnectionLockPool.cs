using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql.ConnectionMultiplexing
{
    internal sealed class MultiplexedConnectionLockPool
    {
        /// <summary>
        /// Protects access to <see cref="connectionStringPools"/> and <see cref="cleanupTask"/>
        /// </summary>
        private readonly object @lock = new object();
        private readonly Dictionary<string, Queue<MultiplexedConnectionLock>> connectionStringPools = new Dictionary<string, Queue<MultiplexedConnectionLock>>();
        private Task cleanupTask = Task.FromResult(false);
        
        public IDisposable TryAcquire<TLockCookie>(
            string connectionString,
            string lockName,
            int timeoutMillis,
            ISqlSynchronizationStrategy<TLockCookie> strategy)
            where TLockCookie : class
        {
            // opportunistic phase: see if we can use a connection that is already holding a lock
            // to acquire the current lock
            var existingLock = this.GetExistingLockOrDefault(connectionString);
            if (existingLock != null)
            {
                try
                {
                    var opportunisticResult = existingLock.TryAcquire(lockName, timeoutMillis, strategy, opportunistic: true);
                    if (opportunisticResult.Handle != null) { return opportunisticResult.Handle; }
                    
                    switch (opportunisticResult.Retry)
                    {
                        case MultiplexedConnectionLockRetry.NoRetry:
                            return null;
                        case MultiplexedConnectionLockRetry.RetryOnThisLock:
                            var result = existingLock.TryAcquire(lockName, timeoutMillis, strategy, opportunistic: false);
                            return result.Handle;
                        case MultiplexedConnectionLockRetry.Retry:
                            break;
                        default:
                            throw new InvalidOperationException("unexpected retry");
                    }
                }
                finally
                {
                    // since we took this lock from the pool, always return it to the pool
                    this.AddLockToPool(connectionString, existingLock);
                }
            }
             
            // normal phase: if we were not able to be opportunistic, ensure that we have a lock
            var @lock = new MultiplexedConnectionLock(connectionString);
            IDisposable handle = null;
            try
            {
                handle = @lock.TryAcquire(lockName, timeoutMillis, strategy, opportunistic: false).Handle;
            }
            finally
            {
                // only store the lock on success; otherwise there's no reason to keep it around
                if (handle != null) { this.AddLockToPool(connectionString, @lock); }
                else { @lock.Dispose(); }
            }
            return handle;
        }

        public async Task<IDisposable> TryAcquireAsync<TLockCookie>(
            string connectionString,
            string lockName,
            int timeoutMillis,
            ISqlSynchronizationStrategy<TLockCookie> strategy,
            CancellationToken cancellationToken)
            where TLockCookie : class
        {
            // opportunistic phase: see if we can use a connection that is already holding a lock
            // to acquire the current lock
            var existingLock = this.GetExistingLockOrDefault(connectionString);
            if (existingLock != null)
            {
                try
                {
                    var opportunisticResult = await existingLock.TryAcquireAsync(lockName, timeoutMillis, strategy, cancellationToken, opportunistic: true).ConfigureAwait(false);
                    if (opportunisticResult.Handle != null) { return opportunisticResult.Handle; }

                    switch (opportunisticResult.Retry)
                    {
                        case MultiplexedConnectionLockRetry.NoRetry:
                            return null;
                        case MultiplexedConnectionLockRetry.RetryOnThisLock:
                            var result = await existingLock.TryAcquireAsync(lockName, timeoutMillis, strategy, cancellationToken, opportunistic: false).ConfigureAwait(false);
                            return result.Handle;
                        case MultiplexedConnectionLockRetry.Retry:
                            break;
                        default:
                            throw new InvalidOperationException("unexpected retry");
                    }
                }
                finally
                {
                    // since we took this lock from the pool, always return it to the pool
                    this.AddLockToPool(connectionString, existingLock);
                }
            }

            // normal phase: if we were not able to be opportunistic, ensure that we have a lock
            var @lock = new MultiplexedConnectionLock(connectionString);
            IDisposable handle = null;
            try
            {
                handle = (await @lock.TryAcquireAsync(lockName, timeoutMillis, strategy, cancellationToken, opportunistic: false).ConfigureAwait(false)).Handle;
            }
            finally
            {
                // only store the lock on success; otherwise there's no reason to keep it around
                if (handle != null) { this.AddLockToPool(connectionString, @lock); }
                else { @lock.Dispose(); }
            }
            return handle;
        }

        private MultiplexedConnectionLock GetExistingLockOrDefault(string connectionString)
        {
            lock (this.@lock)
            {
                Queue<MultiplexedConnectionLock> pool;
                return this.connectionStringPools.TryGetValue(connectionString, out pool)
                    && pool.Count > 0
                    ? pool.Dequeue()
                    : null;
            }
        }

        private void AddLockToPool(string connectionString, MultiplexedConnectionLock @lock)
        {
            lock (this.@lock)
            {
                Queue<MultiplexedConnectionLock> existingPool;
                if (this.connectionStringPools.TryGetValue(connectionString, out existingPool))
                {
                    existingPool.Enqueue(@lock);
                }
                else
                {
                    var newPool = new Queue<MultiplexedConnectionLock>();
                    newPool.Enqueue(@lock);
                    this.connectionStringPools.Add(connectionString, newPool);

                    // if we're adding the first queue, start the cleanup thread
                    if (this.connectionStringPools.Count == 1)
                    {
                        // rather than replacing cleanup task, we continue on it. This ensures that we never end up in a state
                        // where two cleanup tasks are running at once. Since only cleanup can remove from the pools map, we'll
                        // never end up queueing up multiple cleanup tasks on top of one another
                        this.cleanupTask.ContinueWith(
                            (_, state) => Task.Run(() => ((MultiplexedConnectionLockPool)state).CleanupLoop()),
                            state: this,
                            continuationOptions: TaskContinuationOptions.ExecuteSynchronously
                        );
                    }
                }
            }
        }

        #region ---- Cleanup ----
        // mutable for testing purposes
        public static int CleanupIntervalSeconds { get; set; } = 15;

        private async Task CleanupLoop()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(CleanupIntervalSeconds)).ConfigureAwait(false);

                    if (!await this.DoCleanupAsync().ConfigureAwait(false))
                    {
                        return; // exit
                    }
                }
                catch { /* if we hit an error, just keep going */ }
            }
        }

        private async Task<bool> DoCleanupAsync()
        {
            // take a snapshot of the pools so that we can loop over it without holding a lock
            KeyValuePair<string, Queue<MultiplexedConnectionLock>>[] connectionStringPoolsSnapshot;     
            lock (this.@lock)
            {
                connectionStringPoolsSnapshot = this.connectionStringPools.ToArray();
            }
            
            // clean up each pool in the snapshot, noting pools that were empty after cleanup
            List<KeyValuePair<string, Queue<MultiplexedConnectionLock>>> toRemove = null;
            foreach (var kvp in connectionStringPoolsSnapshot)
            {
                await this.DoCleanupAsync(kvp.Value).ConfigureAwait(false);
                if (kvp.Value.Count == 0)
                {
                    (toRemove ?? (toRemove = new List<KeyValuePair<string, Queue<MultiplexedConnectionLock>>>())).Add(kvp);
                }
            }

            // remove pools as necessary
            if (toRemove != null)
            {
                lock (this.@lock)
                {
                    foreach (var poolToRemove in toRemove)
                    {
                        // we check IsEmpty again since the pool could have been added to
                        // while we weren't holding the lock
                        if (poolToRemove.Value.Count == 0)
                        {
                            this.connectionStringPools.Remove(poolToRemove.Key);
                        }
                    }

                    // exit cleanup if we've removed all pools. It's important to do this
                    // inside the same lock as the remove statements since otherwise something
                    // else could be added
                    if (this.connectionStringPools.Count == 0)
                    {
                        return false;
                    }
                }
            }

            // continue cleaning
            return true;
        }

        private async Task DoCleanupAsync(Queue<MultiplexedConnectionLock> lockPool)
        {
            // loop over the entire pool by removing, cleaning, and then returning each active
            // entry. To avoid looping forever, we grab the count at the beginning and run at most
            // that many times. Because entries can be added to and removed from the pool concurrently with
            // this loop, we may end up processing a lock twice or not processing some locks. That's fine.

            int initialCount;
            lock (this.@lock) { initialCount = lockPool.Count; }
            
            for (var i = 0; i < initialCount; ++i)
            {
                MultiplexedConnectionLock toClean;
                lock (this.@lock)
                {
                    if (lockPool.Count == 0) { return; }
                    toClean = lockPool.Dequeue();
                }

                bool shouldReturn;
                try { shouldReturn = await toClean.CleanupAsync().ConfigureAwait(false); }
                // unclear what to do if cleanup fails. For now, we return to the pool in hopes that it will succeed later
                catch { shouldReturn = true; }
                
                if (shouldReturn)
                {
                    lock (this.@lock) { lockPool.Enqueue(toClean); }
                }
                else
                {
                    toClean.Dispose();
                }
            }
        }
        #endregion
    }
}
