using System.Runtime.CompilerServices;

namespace Medallion.Threading.Internal;

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

    public static async ValueTask ConvertToVoid<TResult>(this ValueTask<TResult> task) => await task.ConfigureAwait(false);

    public static ValueTask<T> AsValueTask<T>(this Task<T> task) => new(task);
    public static ValueTask AsValueTask(this Task task) => new(task);
    public static ValueTask<T> AsValueTask<T>(this T value) => new(value);

    public static Task<TResult> SafeCreateTask<TState, TResult>(Func<TState, Task<TResult>> taskFactory, TState state) =>
        InternalSafeCreateTask<TState, Task<TResult>, TResult>(taskFactory, state);

    public static Task SafeCreateTask<TState>(Func<TState, Task> taskFactory, TState state) =>
        InternalSafeCreateTask<TState, Task, bool>(taskFactory, state);

    private static TTask InternalSafeCreateTask<TState, TTask, TResult>(Func<TState, TTask> taskFactory, TState state)
        where TTask : Task
    {
        try { return taskFactory(state); }
        catch (OperationCanceledException)
        {
            // don't use Task.FromCanceled here because oce.CancellationToken is not guaranteed to 
            // have IsCancellationRequested which FromCanceled requires
            var canceledTaskBuilder = new TaskCompletionSource<TResult>();
            canceledTaskBuilder.SetCanceled();
            return (TTask)canceledTaskBuilder.Task.As<object>();
        }
        catch (Exception ex) { return (TTask)Task.FromException<TResult>(ex).As<object>(); }
    }

    public static ObjectDisposedException ObjectDisposed<T>(this T _) where T : IAsyncDisposable =>
        throw new ObjectDisposedException(typeof(T).ToString());

    public static NonThrowingAwaitable<TTask> TryAwait<TTask>(this TTask task) where TTask : Task =>
        new(task);

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
#if NET8_0_OR_GREATER
            this._taskAwaiter = task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing).GetAwaiter();
#else
            this._taskAwaiter = task.ConfigureAwait(false).GetAwaiter();
#endif
        }

        public NonThrowingAwaitable<TTask> GetAwaiter() => this;

        public bool IsCompleted => this._taskAwaiter.IsCompleted;

        public TTask GetResult()
        {
            Invariant.Require(this._task.IsCompleted);

#if NET8_0_OR_GREATER
            this._taskAwaiter.GetResult();
#else
            // Does NOT call _taskAwaiter.GetResult() since that could throw!
            // We do, however, access the Exception property to avoid hitting UnobservedTaskException.
            if (this._task.IsFaulted) { _ = this._task.Exception; }
#endif

            return this._task;
        }

        public void OnCompleted(Action continuation) => this._taskAwaiter.OnCompleted(continuation);
        public void UnsafeOnCompleted(Action continuation) => this._taskAwaiter.UnsafeOnCompleted(continuation);
    }

    public static bool TryGetValue<T>(this T? nullable, out T value)
        where T : struct
    {
        value = nullable.GetValueOrDefault();
        return nullable.HasValue;
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
