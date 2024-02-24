using Medallion.Threading.Internal;
using StackExchange.Redis;

namespace Medallion.Threading.Redis.RedLock;

internal interface IRedLockReleasableSynchronizationPrimitive
{
    Task ReleaseAsync(IDatabaseAsync database, bool fireAndForget);
    void Release(IDatabase database, bool fireAndForget);
}

/// <summary>
/// Implements the release operation in the RedLock algorithm. See https://redis.io/topics/distlock
/// </summary>
internal readonly struct RedLockRelease(
    IRedLockReleasableSynchronizationPrimitive primitive,
    IReadOnlyDictionary<IDatabase, Task<bool>> tryAcquireOrRenewTasks)
{
    public async ValueTask ReleaseAsync()
    {
        var isSynchronous = SyncViaAsync.IsSynchronous;
        var unreleasedTryAcquireOrRenewTasks = tryAcquireOrRenewTasks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        
        List<Exception>? releaseExceptions = null;
        var successCount = 0;
        var faultCount = 0;
        var databaseCount = unreleasedTryAcquireOrRenewTasks.Count;

        try
        {
            while (true)
            {
                var releaseableDatabases = unreleasedTryAcquireOrRenewTasks.Where(kvp => kvp.Value.IsCompleted)
                    // work through completed tasks first TODO necessary?
                    .OrderByDescending(kvp => kvp.Value.IsCompleted)
                    // among those prioritize successful completions since faults are likely to be slow to process
                    .ThenByDescending(kvp => kvp.Value.Status == TaskStatus.RanToCompletion)
                    // among those prioritize failed (not faulted) acquisitions since those require no work to release
                    .ThenByDescending(kvp => RedLockHelper.ReturnedFalse(kvp.Value))
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
                            if (isSynchronous) { primitive.Release(db, fireAndForget: false); }
                            else { await primitive.ReleaseAsync(db, fireAndForget: false).ConfigureAwait(false); }
                            ++successCount;
                        }
                        catch (Exception ex) 
                        {
                            (releaseExceptions ??= []).Add(ex);
                            ++faultCount;
                            if (RedLockHelper.HasTooManyFailuresOrFaults(faultCount, databaseCount))
                            {
                                throw new AggregateException(releaseExceptions!).Flatten();
                            }
                        }
                    }

                    if (RedLockHelper.HasSufficientSuccesses(successCount, databaseCount))
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
                RedLockHelper.FireAndForgetReleaseUponCompletion(primitive, kvp.Key, kvp.Value);
            }
        }
    }
}
