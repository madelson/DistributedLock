using Medallion.Threading.Internal;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Redis.RedLock;

internal static class RedLockHelper
{
    private static readonly string LockIdPrefix;

    static RedLockHelper()
    {
        using var currentProcess = Process.GetCurrentProcess();
        LockIdPrefix = $"{Environment.MachineName}_{currentProcess.Id}_";
    }

    public static bool HasSufficientSuccesses(int successCount, int databaseCount)
    {
        // a majority is required
        var threshold = (databaseCount / 2) + 1;
        // While in theory this should return true if we have more than enough, we never expect this to be
        // called except with just enough or not enough due to how we've implemented our approaches.
        Invariant.Require(successCount <= threshold);
        return successCount >= threshold;
    }

    public static bool HasTooManyFailuresOrFaults(int failureOrFaultCount, int databaseCount)
    {
        // For an odd number of databases, we need a majority to make success impossible. For an
        // even number, however, getting to 50% failures/faults is sufficient to rule out getting
        // a majority of successes.
        var threshold = (databaseCount / 2) + (databaseCount % 2);
        // While in theory this should return true if we have more than enough, we never expect this to be
        // called except with just enough or not enough due to how we've implemented our approaches.
        Invariant.Require(failureOrFaultCount <= threshold);
        return failureOrFaultCount >= threshold;
    }

    public static RedisValue CreateLockId() => LockIdPrefix + Guid.NewGuid().ToString("n");

    public static bool ReturnedFalse(Task<bool> task) => task.Status == TaskStatus.RanToCompletion && !task.Result;

    public static void FireAndForgetReleaseUponCompletion(IRedLockReleasableSynchronizationPrimitive primitive, IDatabase database, Task<bool> acquireOrRenewTask)
    {
        if (ReturnedFalse(acquireOrRenewTask)) { return; }

        acquireOrRenewTask.ContinueWith(async (t, state) =>
            {
                // don't clean up if we know we failed
                if (!ReturnedFalse(t))
                {
                    await primitive.ReleaseAsync((IDatabase) state, fireAndForget: true).ConfigureAwait(false);
                }
            },
            state: database
        );
    }

    public static CommandFlags GetCommandFlags(bool fireAndForget) => 
        CommandFlags.DemandMaster | (fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None);

    public static async Task<bool> AsBooleanTask(this Task<RedisResult> redisResultTask) => (bool)await redisResultTask.ConfigureAwait(false);
}
