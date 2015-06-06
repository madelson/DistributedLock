using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    internal static class WaitHandleHelpers
    {
        public static Task<bool> WaitOneAsync(this WaitHandle @this, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (@this == null)
                throw new ArgumentNullException("this");
            var timeoutMillis = timeout.ToInt32Timeout();

            return WaitOneAsync(@this, timeoutMillis, cancellationToken);
        }

        // based on http://www.thomaslevesque.com/2015/06/04/async-and-cancellation-support-for-wait-handles/
        private static async Task<bool> WaitOneAsync(WaitHandle handle, int timeoutMillis, CancellationToken cancellationToken)
        {
            RegisteredWaitHandle registeredHandle = null;
            CancellationTokenRegistration tokenRegistration = default(CancellationTokenRegistration);
            try
            {
                var taskCompletionSource = new TaskCompletionSource<bool>();
                registeredHandle = ThreadPool.RegisterWaitForSingleObject(
                    handle,
                    (state, timedOut) => ((TaskCompletionSource<bool>)state).TrySetResult(!timedOut),
                    state: taskCompletionSource,
                    millisecondsTimeOutInterval: timeoutMillis,
                    executeOnlyOnce: true
                );
                tokenRegistration = cancellationToken.Register(
                    state => ((TaskCompletionSource<bool>)state).TrySetCanceled(),
                    state: taskCompletionSource
                );
                return await taskCompletionSource.Task.ConfigureAwait(false);
            }
            finally
            {
                if (registeredHandle != null)
                    registeredHandle.Unregister(handle);
                tokenRegistration.Dispose();
            }
        }
    }
}
