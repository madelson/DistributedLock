using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal
{
    /// <summary>
    /// Helps re-use code across sync and async pathways, leveraging the fact that async code will run synchronously
    /// unless it actually encounters an async operation. Downstream code should use the <see cref="IsSynchronous"/>
    /// to choose between sync and async operations
    /// </summary>
#if DEBUG
    public
#else
    internal
#endif
    static class SyncOverAsync
    {
        [ThreadStatic]
        private static bool _isSynchronous;

        public static bool IsSynchronous => _isSynchronous;

        // todo get rid of WillGoAsync, replace with a debug-only API for turning on these assertions that a test can use
        // e. g. using (SyncOverAsync.SyncMode()) { Assert.DoesNotThrow(() => handle.Dispose()); }

        public static void Run<TState>(Func<TState, ValueTask> action, TState state, bool willGoAsync)
        {
            Run(
                async s =>
                {
                    await s.action(s.state).ConfigureAwait(false);
                    return true;
                },
                (action, state),
                willGoAsync
            );
        }

        public static TResult Run<TState, TResult>(Func<TState, ValueTask<TResult>> action, TState state, bool willGoAsync)
        {
            Invariant.Require(!_isSynchronous);

            if (willGoAsync)
            {
                var task = action(state);
                Invariant.Require(!task.IsCompleted);
                // call AsTask(), since https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1?view=netcore-3.1
                // says that we should not call GetAwaiter().GetResult() except on a completed ValueTask
                return task.AsTask().GetAwaiter().GetResult();
            }

            try
            {
                _isSynchronous = true;

                var task = action(state);
                Invariant.Require(task.IsCompleted);
                return task.GetAwaiter().GetResult();
            }
            finally
            {
                _isSynchronous = false;
            }
        }

        public static ValueTask Delay(TimeoutValue timeout, CancellationToken cancellationToken)
        {
            if (!IsSynchronous)
            {
                return Task.Delay(timeout.InMilliseconds, cancellationToken).AsValueTask();
            }

            if (cancellationToken.CanBeCanceled)
            {
                if (cancellationToken.WaitHandle.WaitOne(timeout.InMilliseconds))
                {
                    throw new OperationCanceledException("delay was canceled", cancellationToken);
                }
            }
            else
            {
                Thread.Sleep(timeout.InMilliseconds);
            }

            return default;
        }
    }
}
