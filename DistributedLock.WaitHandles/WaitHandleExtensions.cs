using Medallion.Threading.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.WaitHandles
{
    internal static class WaitHandleExtensions
    {
        public static async ValueTask<bool> WaitOneAsync(this WaitHandle waitHandle, TimeoutValue timeout, CancellationToken cancellationToken)
        {
            return SyncOverAsync.IsSynchronous
                ? waitHandle.InternalWaitOne(timeout, cancellationToken)
                : await waitHandle.InternalWaitOneAsync(timeout, cancellationToken).ConfigureAwait(false);
        }

        private static bool InternalWaitOne(this WaitHandle waitHandle, TimeoutValue timeout, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                return waitHandle.WaitOne(timeout.InMilliseconds);
            }

            // if, upon entering the method we are already both canceled and signaled, this check
            // ensures that we cancel
            cancellationToken.ThrowIfCancellationRequested();

            // cancellable wait based on
            // http://www.thomaslevesque.com/2015/06/04/async-and-cancellation-support-for-wait-handles/
            var index = WaitHandle.WaitAny(new[] { waitHandle, cancellationToken.WaitHandle }, timeout.InMilliseconds);
            switch (index)
            {
                case WaitHandle.WaitTimeout: // timeout
                    return false;
                case 0: // event
                    return true;
                default: // canceled
                    throw new OperationCanceledException(cancellationToken);
            }
        }

        // based on http://www.thomaslevesque.com/2015/06/04/async-and-cancellation-support-for-wait-handles/
        private static async ValueTask<bool> InternalWaitOneAsync(this WaitHandle waitHandle, TimeoutValue timeout, CancellationToken cancellationToken)
        {
            RegisteredWaitHandle? registeredHandle = null;
            CancellationTokenRegistration tokenRegistration = default;
            try
            {
                var taskCompletionSource = new TaskCompletionSource<bool>();
                // if, upon entering the method we are already both canceled and signaled,
                // putting this first ensures that we cancel
                tokenRegistration = cancellationToken.Register(
                    state => ((TaskCompletionSource<bool>)state).TrySetCanceled(),
                    state: taskCompletionSource
                );
                registeredHandle = ThreadPool.RegisterWaitForSingleObject(
                    waitHandle,
                    (state, timedOut) => ((TaskCompletionSource<bool>)state).TrySetResult(!timedOut),
                    state: taskCompletionSource,
                    millisecondsTimeOutInterval: timeout.InMilliseconds,
                    executeOnlyOnce: true
                );
                return await taskCompletionSource.Task.ConfigureAwait(false);
            }
            finally
            {
                // this is different from the referenced site, but I think this is more correct:
                // the handle passed to unregister is a handle to be signaled, not the one to unregister
                // (that one is already captured by the registered handle). See
                // http://referencesource.microsoft.com/#mscorlib/system/threading/threadpool.cs,065408fc096354fd
                registeredHandle?.Unregister(null);
                tokenRegistration.Dispose();
            }
        }
    }
}
