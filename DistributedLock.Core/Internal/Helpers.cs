using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal
{
#if DEBUG
    public
#else
    internal
#endif
    static class Helpers
    {
        /// <summary>
        /// Performs a type-safe cast
        /// </summary>
        public static T As<T>(this T @this) => @this;

        /// <summary>
        /// Performs a type-safe "cast" of a <see cref="ValueTask{TResult}"/>
        /// </summary>
        public static async ValueTask<TBase> Convert<TDerived, TBase>(this ValueTask<TDerived> task, To<TBase>.ValueTaskConversion _)
            where TDerived : TBase =>
            await task.ConfigureAwait(false);

        public readonly struct TaskConversion
        {
            public TaskConversion<TTo> To<TTo>() => throw new InvalidOperationException();
        }

        public readonly struct TaskConversion<TTo> { }

        internal static async ValueTask ConvertToVoid<TResult>(this ValueTask<TResult> task) => await task.ConfigureAwait(false);

        public static ValueTask<T> AsValueTask<T>(this Task<T> task) => new ValueTask<T>(task);
        public static ValueTask AsValueTask(this Task task) => new ValueTask(task);
        public static ValueTask<T> AsValueTask<T>(this T value) => new ValueTask<T>(value);

        // todo rethink message here; should this return "handle" or something more generic since it might be an internal type?
        public static ObjectDisposedException ObjectDisposed<T>(this T _) where T : IAsyncDisposable =>
            throw new ObjectDisposedException(typeof(T).ToString());

        public static NonThrowingAwaitable<TTask> TryAwait<TTask>(this TTask task) where TTask : Task =>
            new NonThrowingAwaitable<TTask>(task);

        /// <summary>
        /// Throwing exceptions is slow and our workflow has us canceling tasks in the common case. Using this special awaitable
        /// allows for us to await those tasks without causing a thrown exception
        /// </summary>
        public readonly struct NonThrowingAwaitable<TTask> : ICriticalNotifyCompletion
            where TTask : Task
        {
            private readonly TTask _task;
            private readonly ConfiguredTaskAwaitable.ConfiguredTaskAwaiter _taskAwaiter;

            public NonThrowingAwaitable(TTask task)
            {
                this._task = task;
                this._taskAwaiter = task.ConfigureAwait(false).GetAwaiter();
            }

            public NonThrowingAwaitable<TTask> GetAwaiter() => this;

            public bool IsCompleted => this._taskAwaiter.IsCompleted;

            public TTask GetResult()
            {
                // does NOT call _taskAwaiter.GetResult() since that could throw!

                Invariant.Require(this._task.IsCompleted);
                return this._task;
            }

            public void OnCompleted(Action continuation) => this._taskAwaiter.OnCompleted(continuation);
            public void UnsafeOnCompleted(Action continuation) => this._taskAwaiter.UnsafeOnCompleted(continuation);
        }
    }

    /// <summary>
    /// Assists with type inference for value task conversions
    /// </summary>
#if DEBUG
    public
#else
    internal
#endif
    static class To<TTo>
    {
        public static ValueTaskConversion ValueTask => default;

        public readonly struct ValueTaskConversion { }
    }
}
