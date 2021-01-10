using Medallion.Threading.Internal;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Redis.RedLock
{
    internal interface IRedLockReleasableSynchronizationPrimitive
    {
        Task ReleaseAsync(IDatabaseAsync database, bool fireAndForget);
        void Release(IDatabase database, bool fireAndForget);
    }

    /// <summary>
    /// Implements the release operation in the RedLock algorithm. See https://redis.io/topics/distlock
    /// </summary>
    internal readonly struct RedLockRelease
    {
        private readonly IRedLockReleasableSynchronizationPrimitive _primitive;
        private readonly IReadOnlyDictionary<IDatabase, Task<bool>> _tryAcquireOrRenewTasks;
        
        public RedLockRelease(
            IRedLockReleasableSynchronizationPrimitive primitive,
            IReadOnlyDictionary<IDatabase, Task<bool>> tryAcquireOrRenewTasks)
        {
            this._primitive = primitive;
            this._tryAcquireOrRenewTasks = tryAcquireOrRenewTasks;
        }

        public async ValueTask ReleaseAsync()
        {
            var isSynchronous = SyncViaAsync.IsSynchronous;
            var unreleasedTryAcquireOrRenewTasks = this._tryAcquireOrRenewTasks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            List<Exception>? releaseExceptions = null;
            var successCount = 0;
            var faultCount = 0;
            var threshold = (unreleasedTryAcquireOrRenewTasks.Count / 2) + 1;

            try
            {
                while (true)
                {
                    var releaseableDatabases = unreleasedTryAcquireOrRenewTasks.Where(kvp => kvp.Value.IsCompleted)
                        // work through non-faulted tasks first
                        .OrderByDescending(kvp => kvp.Value.Status == TaskStatus.RanToCompletion)
                        // then start with failed since no action is required to release those
                        .ThenBy(kvp => kvp.Value.Status == TaskStatus.RanToCompletion && kvp.Value.Result)
                        .Select(kvp => kvp.Key)
                        .ToArray();
                    foreach (var db in releaseableDatabases)
                    {
                        var tryAcquireOrRenewTask = unreleasedTryAcquireOrRenewTasks[db];
                        unreleasedTryAcquireOrRenewTasks.Remove(db);

                        if (RedLockHelper.ReturnedFalse(tryAcquireOrRenewTask))
                        {
                            // if we failed to acquire, we don't need to release
                            ++successCount;
                        }
                        else
                        {
                            try
                            {
                                if (isSynchronous) { this._primitive.Release(db, fireAndForget: false); }
                                else { await this._primitive.ReleaseAsync(db, fireAndForget: false).ConfigureAwait(false); }
                                ++successCount;
                            }
                            catch (Exception ex) 
                            {
                                (releaseExceptions ??= new List<Exception>()).Add(ex);
                                ++faultCount;
                                if (faultCount >= threshold)
                                {
                                    throw new AggregateException(releaseExceptions!).Flatten();
                                }
                            }
                        }

                        if (successCount >= threshold)
                        {
                            return;
                        }
                    }

                    // if we haven't released enough yet to be done or certain of success or failure, wait for another to finish
                    if (isSynchronous) { Task.WaitAny(unreleasedTryAcquireOrRenewTasks.Values.ToArray()); }
                    else { await Task.WhenAny(unreleasedTryAcquireOrRenewTasks.Values).ConfigureAwait(false); }
                }
            }
            finally // fire and forget the rest
            {
                foreach (var kvp in unreleasedTryAcquireOrRenewTasks)
                {
                    RedLockHelper.FireAndForgetReleaseUponCompletion(this._primitive, kvp.Key, kvp.Value);
                }
            }
        }
    }
}
