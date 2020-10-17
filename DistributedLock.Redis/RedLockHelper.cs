using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Redis
{
    internal static class RedLockHelper
    {
        public static bool ReturnedFalse(Task<bool> task) => task.Status == TaskStatus.RanToCompletion && !task.Result;

        public static void FireAndForgetReleaseUponCompletion(IRedisSynchronizationPrimitive primitive, IDatabase database, Task<bool> acquireOrRenewTask)
        {
            if (ReturnedFalse(acquireOrRenewTask)) { return; }

            acquireOrRenewTask.ContinueWith(async (t, state) =>
                {
                    // don't clean up if we know we failed
                    if (!ReturnedFalse(t))
                    {
                        await primitive.ReleaseAsync((IDatabase)state, fireAndForget: true).ConfigureAwait(false);
                    }
                },
                state: database
            );
        }
    }
}
