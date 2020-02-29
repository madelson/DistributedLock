using System;
using System.Collections.Generic;
using System.Text;
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

        public static void Run<TState>(Func<TState, ValueTask> action, TState state, bool willGoAsync)
        {
            Invariant.Require(!_isSynchronous);

            if (willGoAsync)
            {
                var task = action(state);
                Invariant.Require(!task.IsCompleted);
                // call AsTask(), since https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1?view=netcore-3.1
                // says that we should not call GetAwaiter().GetResult() except on a completed ValueTask
                task.AsTask().GetAwaiter().GetResult();
                return;
            }

            try
            {
                _isSynchronous = true;

                var task = action(state);
                Invariant.Require(task.IsCompleted);
            }
            finally
            {
                _isSynchronous = false;
            }
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
    }
}
