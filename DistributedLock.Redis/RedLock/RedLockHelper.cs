using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Redis.RedLock
{
    internal static class RedLockHelper
    {
        private static readonly string LockIdPrefix;

        static RedLockHelper()
        {
            using var currentProcess = Process.GetCurrentProcess();
            LockIdPrefix = $"{Environment.MachineName}_{currentProcess.Id}_";
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
}
