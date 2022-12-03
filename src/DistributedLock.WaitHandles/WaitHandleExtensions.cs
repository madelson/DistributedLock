using Medallion.Threading.Internal;

namespace Medallion.Threading.WaitHandles;

internal static class WaitHandleExtensions
{
    public static async ValueTask<bool> WaitOneAsync(this WaitHandle waitHandle, TimeoutValue timeout, CancellationToken cancellationToken)
    {
        if (timeout.IsZero || SyncViaAsync.IsSynchronous)
        {
            return waitHandle.InternalWaitOne(timeout, cancellationToken);
        }

        // when doing an async wait, still do a quick sync check first with timeout zero to optimize the already-signaled case
        return waitHandle.InternalWaitOne(TimeSpan.Zero, cancellationToken) 
            || await waitHandle.InternalWaitOneAsync(timeout, cancellationToken).ConfigureAwait(false);
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

        // optimize the already-signaled case
        if (waitHandle.WaitOne(TimeSpan.Zero))
        {
            return true;
        }
        if (timeout.IsZero)
        {
            return false;
        }

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
        Invariant.Require(!cancellationToken.CanBeCanceled || waitHandle is EventWaitHandle or Semaphore); // keep in sync with Resignal()

        var taskCompletionSource = new TaskCompletionSource<bool>();

        RegisteredWaitHandle? registeredHandle = null;
        CancellationTokenRegistration tokenRegistration = default;
        try
        {
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
            if (registeredHandle != null)
            {
                if (taskCompletionSource.Task.IsCanceled)
                {
                    // If the task got canceled, then there is a slim chance of a race condition where
                    // the wait callback is still running, and hasn't re-signaled the handle yet. If we
                    // return before that point then we might dispose the handle, before getting to re-signal
                    // it. To prevent that, we pass in an MRE which will be signaled when the reservation fully
                    // completes and we wait for that signal before returning.
                    using ManualResetEvent unregisterCompleteEvent = new(initialState: false);
                    registeredHandle.Unregister(unregisterCompleteEvent);
                    await unregisterCompleteEvent.WaitOneAsync(Timeout.InfiniteTimeSpan, CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    registeredHandle.Unregister(null);
                }
            }
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
