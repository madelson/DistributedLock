using Medallion.Threading.Internal;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Redis.RedLock
{
    internal interface IRedLockExtensibleSynchronizationPrimitive  : IRedLockReleasableSynchronizationPrimitive
    {
        TimeoutValue AcquireTimeout { get; }
        Task<bool> TryExtendAsync(IDatabaseAsync database);
    }

    /// <summary>
    /// Implements the extend operation in the RedLock algorithm. See https://redis.io/topics/distlock
    /// </summary>
    internal readonly struct RedLockExtend
    {
        private readonly IRedLockExtensibleSynchronizationPrimitive _primitive;
        private readonly Dictionary<IDatabase, Task<bool>> _tryAcquireOrRenewTasks;
        private readonly CancellationToken _cancellationToken;

        public RedLockExtend(
            IRedLockExtensibleSynchronizationPrimitive primitive, 
            Dictionary<IDatabase, Task<bool>> tryAcquireOrRenewTasks, 
            CancellationToken cancellationToken)
        {
            this._primitive = primitive;
            this._tryAcquireOrRenewTasks = tryAcquireOrRenewTasks;
            this._cancellationToken = cancellationToken;
        }

        public async Task<bool?> TryExtendAsync()
        {
            Invariant.Require(!SyncViaAsync.IsSynchronous, "should only be called from a background renewal thread which is async");

            var incompleteTasks = new HashSet<Task>();
            foreach (var kvp in this._tryAcquireOrRenewTasks.ToArray())
            {
                if (kvp.Value.IsCompleted)
                {
                    incompleteTasks.Add(
                        this._tryAcquireOrRenewTasks[kvp.Key] = Helpers.SafeCreateTask(
                            state => state.primitive.TryExtendAsync(state.database), 
                            (primitive: this._primitive, database: kvp.Key)
                        )
                    );
                }
                else
                {
                    // if the previous acquire/renew is still going, just keep waiting for that
                    incompleteTasks.Add(kvp.Value);
                }
            }

            // For extension we use the same timeout as acquire. This ensures the same min validity time which should be
            // sufficient to keep extending
            using var timeout = new TimeoutTask(this._primitive.AcquireTimeout, this._cancellationToken);
            incompleteTasks.Add(timeout.Task);

            var threshold = (this._tryAcquireOrRenewTasks.Count / 2) + 1;
            var successCount = 0;
            var failCount = 0;
            while (true)
            {
                var completed = await Task.WhenAny(incompleteTasks).ConfigureAwait(false);

                if (completed == timeout.Task)
                {
                    await completed.ConfigureAwait(false); // propagate cancellation
                    return null; // inconclusive
                }
                
                if (completed.Status == TaskStatus.RanToCompletion && ((Task<bool>)completed).Result)
                {
                    ++successCount;
                    if (successCount >= threshold) { return true; } 
                }
                else
                {
                    // note that we treat faulted and failed the same in extend. There's no reason to throw, since
                    // this is just called by the extend loop. While in theory a fault could indicate some kind of post-success
                    // failure, most likely it means the db is unreachable and so it is safest to consider it a failure
                    ++failCount;
                    if (failCount >= threshold) { return false; }
                }
            }
        }
    }
}
