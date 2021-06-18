using Medallion.Threading.Internal;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Redis.RedLock
{
    internal interface IRedLockAcquirableSynchronizationPrimitive : IRedLockReleasableSynchronizationPrimitive
    {
        TimeoutValue AcquireTimeout { get; }
        Task<bool> TryAcquireAsync(IDatabaseAsync database);
        bool TryAcquire(IDatabase database);
    }

    /// <summary>
    /// Implements the acquire operation in the RedLock algorithm. See https://redis.io/topics/distlock
    /// </summary>
    internal readonly struct RedLockAcquire
    {
        private readonly IRedLockAcquirableSynchronizationPrimitive _primitive;
        private readonly IReadOnlyList<IDatabase> _databases;
        private readonly CancellationToken _cancellationToken;

        public RedLockAcquire(
            IRedLockAcquirableSynchronizationPrimitive primitive, 
            IReadOnlyList<IDatabase> databases,
            CancellationToken cancellationToken)
        {
            this._primitive = primitive;
            this._databases = databases;
            this._cancellationToken = cancellationToken;
        }

        public async ValueTask<Dictionary<IDatabase, Task<bool>>?> TryAcquireAsync()
        {
            this._cancellationToken.ThrowIfCancellationRequested();

            var isSynchronous = SyncViaAsync.IsSynchronous;
            if (isSynchronous && this._databases.Count == 1)
            {
                return this.TrySingleFullySynchronousAcquire();
            }

            var primitive = this._primitive;
            var tryAcquireTasks = this._databases.ToDictionary(
                db => db,
                db => Helpers.SafeCreateTask(state => state.primitive.TryAcquireAsync(state.db), (primitive, db))
            );

            var waitForAcquireTask = this.WaitForAcquireAsync(tryAcquireTasks);

            var succeeded = false;
            try
            {
                succeeded = await waitForAcquireTask.AwaitSyncOverAsync().ConfigureAwait(false);
            }
            finally
            {
                // clean up
                if (!succeeded)
                {
                    List<Task>? releaseTasks = null;
                    foreach (var kvp in tryAcquireTasks)
                    {
                        // if the task hasn't finished yet, we don't want to do any releasing now; just
                        // queue a release command to run when the task eventually completes
                        if (!kvp.Value.IsCompleted)
                        {
                            RedLockHelper.FireAndForgetReleaseUponCompletion(primitive, kvp.Key, kvp.Value);
                        }
                        // otherwise, unless we know we failed to acquire, do a release
                        else if (!RedLockHelper.ReturnedFalse(kvp.Value))
                        {
                            if (isSynchronous)
                            {
                                try { primitive.Release(kvp.Key, fireAndForget: true); }
                                catch { }
                            }
                            else
                            {
                                (releaseTasks ??= new List<Task>())
                                    .Add(Helpers.SafeCreateTask(state => state.primitive.ReleaseAsync(state.Key, fireAndForget: true), (primitive, kvp.Key)));
                            }
                        }
                    }

                    if (releaseTasks != null)
                    {
                        await Task.WhenAll(releaseTasks).ConfigureAwait(false);
                    }
                }
            }

            return succeeded ? tryAcquireTasks : null;
        }

        private async Task<bool> WaitForAcquireAsync(IReadOnlyDictionary<IDatabase, Task<bool>> tryAcquireTasks)
        {
            using var timeout = new TimeoutTask(this._primitive.AcquireTimeout, this._cancellationToken);
            var incompleteTasks = new HashSet<Task>(tryAcquireTasks.Values) { timeout.Task };

            var successCount = 0;
            var failCount = 0;
            var faultCount = 0;
            while (true)
            {
                var completed = await Task.WhenAny(incompleteTasks).ConfigureAwait(false);

                if (completed == timeout.Task)
                {
                    await completed.ConfigureAwait(false); // propagates cancellation
                    return false; // true timeout
                }

                if (completed.Status == TaskStatus.RanToCompletion)
                {
                    var result = await ((Task<bool>)completed).ConfigureAwait(false);
                    if (result)
                    {
                        ++successCount;
                        if (RedLockHelper.HasSufficientSuccesses(successCount, this._databases.Count)) { return true; }
                    }
                    else
                    {
                        ++failCount;
                        if (RedLockHelper.HasTooManyFailuresOrFaults(failCount, this._databases.Count)) { return false; }
                    }
                }
                else // faulted or canceled
                {
                    // if we get too many faults, the lock is not possible to acquire, so we should throw
                    ++faultCount;
                    if (RedLockHelper.HasTooManyFailuresOrFaults(faultCount, this._databases.Count))
                    {
                        var faultingTasks = tryAcquireTasks.Values.Where(t => t.IsCanceled || t.IsFaulted)
                            .ToArray();
                        if (faultingTasks.Length == 1)
                        {
                            await faultingTasks[0].ConfigureAwait(false); // propagate the error
                        }

                        throw new AggregateException(faultingTasks.Select(t => t.Exception ?? new TaskCanceledException(t).As<Exception>()))
                            .Flatten();
                    }

                    ++failCount;
                    if (RedLockHelper.HasTooManyFailuresOrFaults(failCount, this._databases.Count)) { return false; }
                }

                incompleteTasks.Remove(completed);
                Invariant.Require(incompleteTasks.Count > 1, "should be more than just timeout left");
            }
        }

        /// <summary>
        /// We only allow synchronous acquire for a single db because StackExchange.Redis does not currently allow for
        /// single-operation timeouts/cancellations. Therefore, one slow response would jeopardize our ability to claim the
        /// lock in time. With a single db, the one operation is all that matters so it is fine if we need to wait for it.
        /// </summary>
        private Dictionary<IDatabase, Task<bool>>? TrySingleFullySynchronousAcquire()
        {
            var database = this._databases.Single();

            bool success;
            var stopwatch = Stopwatch.StartNew();
            try { success = this._primitive.TryAcquire(database); }
            catch
            {
                // on failure, still attempt a release just in case
                try { this._primitive.Release(database, fireAndForget: true); }
                catch { } // do nothing; we're going to throw anyway and the cause of failure is probably the same

                throw;
            }

            if (success)
            {
                // make sure we didn't time out
                if (this._primitive.AcquireTimeout.CompareTo(stopwatch.Elapsed) >= 0)
                {
                    return new Dictionary<IDatabase, Task<bool>> { [database] = Task.FromResult(success) };
                }

                this._primitive.Release(database, fireAndForget: true); // timed out, so release
            }

            return null;
        }
    }
}
