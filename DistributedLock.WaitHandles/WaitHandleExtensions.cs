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
            return SyncViaAsync.IsSynchronous
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
            return index switch
            {
                // timeout
                WaitHandle.WaitTimeout => false,
                // event
                0 => true,
                // canceled
                _ => throw new OperationCanceledException(cancellationToken),
            };
        }

        // based on http://www.thomaslevesque.com/2015/06/04/async-and-cancellation-support-for-wait-handles/
        private static async ValueTask<bool> InternalWaitOneAsync(this WaitHandle waitHandle, TimeoutValue timeout, CancellationToken cancellationToken)
        {
            Invariant.Require(waitHandle is EventWaitHandle or Semaphore); // keep in sync with Resignal()

            RegisteredWaitHandle? registeredHandle = null;
            CancellationTokenRegistration tokenRegistration = default;
            try
            {
                var taskCompletionSource = new TaskCompletionSource<bool>();
                // if, upon entering the method we are already both canceled and signaled,
                // putting this first ensures that we cancel
                tokenRegistration = cancellationToken.Register(
                    static state => ((TaskCompletionSource<bool>)state).TrySetCanceled(),
                    state: taskCompletionSource
                );
                registeredHandle = ThreadPool.RegisterWaitForSingleObject(
                    waitHandle,
                    static (state, timedOut) => OnSignaled(state, timedOut),
                    state: Tuple.Create(taskCompletionSource, waitHandle),
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

            static void OnSignaled(object state, bool timedOut)
            {
                var (taskCompletionSource, waitHandle) = (Tuple<TaskCompletionSource<bool>, WaitHandle>)state;
                if (!taskCompletionSource.TrySetResult(!timedOut) && !timedOut && taskCompletionSource.Task.IsCanceled)
                {
                    // If we received a signal (not a timeout) and we lost the race with cancellation, resignal
                    // the handle to avoid the signal being lost. See https://github.com/madelson/DistributedLock/issues/120
                    Resignal(waitHandle);
                }
            }
        }

        private static void Resignal(WaitHandle waitHandle)
        {
            try
            {
                if (waitHandle is EventWaitHandle @event)
                {
                    @event.Set();
                }
                else if (waitHandle is Semaphore semaphore)
                {
                    semaphore.Release();
                }
            }
            catch
            {
                // Since this method runs in a threadpool thread, we don't want it to throw
                // even if the methods above fail (e.g. with SemaphoreFullException).
            }
        }
    }
}
