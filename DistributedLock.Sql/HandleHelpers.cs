using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    internal static class HandleHelpers
    {
        public static async ValueTask<THandle?> Wrap<THandle>(ValueTask<IDistributedLockHandle?> handleTask, Func<IDistributedLockHandle, THandle> factory)
            where THandle : class
        {
            var handle = await handleTask.ConfigureAwait(false);
            return handle != null ? factory(handle) : null;
        }
    }
}
