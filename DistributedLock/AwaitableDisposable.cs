using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    public struct AwaitableDisposable<TDisposable> where TDisposable : IDisposable
    {
        internal AwaitableDisposable(Task<TDisposable> task) 
        {
            this.Task = task ?? throw new ArgumentNullException(nameof(task));
        }

        public Task<TDisposable> Task { get; }

        public TaskAwaiter<TDisposable> GetAwaiter() => this.Task.GetAwaiter();
        public ConfiguredTaskAwaitable<TDisposable> ConfigureAwait(bool continueOnCapturedContext) => this.Task.ConfigureAwait(continueOnCapturedContext);

        // todo do we want this?
        public static implicit operator Task<TDisposable>(AwaitableDisposable<TDisposable> source) => source.Task;
    }
}
