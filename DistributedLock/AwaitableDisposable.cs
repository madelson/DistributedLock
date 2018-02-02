using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    /// <summary>
    /// Non-disposable awaitable wrapper type for <see cref="Task{TResult}"/> where <typeparamref name="TDisposable"/> is
    /// <see cref="IDisposable"/>. This uses type-safety to help consumers avoid the easy mistake of disposing the
    /// <see cref="Task{TResult}"/> rather than the underlying <see cref="IDisposable"/>:
    /// 
    /// <code>
    ///     // wrong (won't compile if AcquireAsync() returns AwaitableDisposable)
    ///     using (var handle = myLock.AcquireAsync()) { ... }
    ///     
    ///     // right
    ///     using (var handle = await myLock.AcquireAsync()) { ... }
    /// </code>
    /// </summary>
    public struct AwaitableDisposable<TDisposable> where TDisposable : IDisposable
    {
        /// <summary>
        /// Constructs a new instance of <see cref="AwaitableDisposable{TDisposable}"/> from the given <paramref name="task"/>
        /// </summary>
        public AwaitableDisposable(Task<TDisposable> task) 
        {
            this.Task = task ?? throw new ArgumentNullException(nameof(task));
        }

        /// <summary>
        /// Retrieves the underlying <see cref="Task{TResult}"/> instance
        /// </summary>
        public Task<TDisposable> Task { get; }

        /// <summary>
        /// Implements the awaitable pattern
        /// </summary>
        public TaskAwaiter<TDisposable> GetAwaiter() => this.Task.GetAwaiter();

        /// <summary>
        /// Equivalent to <see cref="Task.ConfigureAwait(bool)"/>
        /// </summary>
        public ConfiguredTaskAwaitable<TDisposable> ConfigureAwait(bool continueOnCapturedContext) => this.Task.ConfigureAwait(continueOnCapturedContext);
    }
}
