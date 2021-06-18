using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal
{
    /// <summary>
    /// Helps re-use code across sync and async pathways, leveraging the fact that async code will run synchronously
    /// unless it actually encounters an async operation. Downstream code should use the <see cref="IsSynchronous"/>
    /// to choose between sync and async operations.
    /// 
    /// This class does not incur the overhead of the sync-over-async anti-pattern; the only overhead is using <see cref="ValueTask"/>s
    /// in a synchronous manner.
    /// </summary>
#if DEBUG
    public
#else
    internal
#endif
    static class SyncViaAsync
    {
        [ThreadStatic]
        private static bool _isSynchronous;

        public static bool IsSynchronous => _isSynchronous;

        /// <summary>
        /// Runs <paramref name="action"/> synchronously
        /// </summary>
        public static void Run<TState>(Func<TState, ValueTask> action, TState state)
        {
            Run(
                async s =>
                {
                    await s.action(s.state).ConfigureAwait(false);
                    return true;
                },
                (action, state)
            );
        }

        /// <summary>
        /// Runs <paramref name="action"/> synchronously
        /// </summary>
        public static TResult Run<TState, TResult>(Func<TState, ValueTask<TResult>> action, TState state)
        {
            Invariant.Require(!_isSynchronous);

            try
            {
                _isSynchronous = true;

                var task = action(state);
                Invariant.Require(task.IsCompleted);

                // this should never happen (and can't in the debug build). However, to make absolutely sure we have this as 
                // fallback logic for the release build
                if (!task.IsCompleted)
                {
                    // call AsTask(), since https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1?view=netcore-3.1
                    // says that we should not call GetAwaiter().GetResult() except on a completed ValueTask
                    return task.AsTask().GetAwaiter().GetResult();
                }

                return task.GetAwaiter().GetResult();
            }
            finally
            {
                _isSynchronous = false;
            }
        }

        /// <summary>
        /// A <see cref="SyncViaAsync"/>-compatible implementation of <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.
        /// </summary>
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

        /// <summary>
        /// For a type <typeparamref name="TDisposable"/> which implements both <see cref="IAsyncDisposable"/> and <see cref="IDisposable"/>,
        /// provides an implementation of <see cref="IDisposable.Dispose"/> using <see cref="IAsyncDisposable.DisposeAsync"/>.
        /// </summary>
        public static void DisposeSyncViaAsync<TDisposable>(this TDisposable disposable)
            where TDisposable : IAsyncDisposable, IDisposable =>
            Run(@this => @this.DisposeAsync(), disposable);

        /// <summary>
        /// In synchronous mode, performs a blocking wait on the provided <paramref name="task"/>. In asynchronous mode,
        /// returns the <paramref name="task"/> as a <see cref="ValueTask{TResult}"/>.
        /// </summary>
        public static ValueTask<TResult> AwaitSyncOverAsync<TResult>(this Task<TResult> task) =>
            IsSynchronous ? task.GetAwaiter().GetResult().AsValueTask() : task.AsValueTask();

        /// <summary>
        /// In synchronous mode, performs a blocking wait on the provided <paramref name="task"/>. In asynchronous mode,
        /// returns the <paramref name="task"/> as a <see cref="ValueTask"/>.
        /// </summary>
        public static ValueTask AwaitSyncOverAsync(this Task task)
        {
            if (IsSynchronous) 
            { 
                task.GetAwaiter().GetResult();
                return default;
            }

            return task.AsValueTask();
        }
    }
}
