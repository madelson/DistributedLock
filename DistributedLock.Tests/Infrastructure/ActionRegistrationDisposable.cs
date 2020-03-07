using System;
using System.Collections.Generic;

namespace Medallion.Threading.Tests
{
    /// <summary>
    /// Utility class to simplify the implementation of testing <see cref="IDisposable"/>s
    /// </summary>
    public abstract class ActionRegistrationDisposable : IDisposable
    {
        private readonly Stack<Action> _cleanupActions = new Stack<Action>();
        private bool disposed;

        public void RegisterCleanupAction(Action action)
        {
            lock (this._cleanupActions)
            {
                if (this.disposed) { throw new ObjectDisposedException(this.GetType().Name); }
                this._cleanupActions.Push(action);
            }
        }

        public static Action CreateWeakDisposeAction(IDisposable disposable) => CreateWeakDisposeAction(new WeakReference<IDisposable>(disposable));

        private static Action CreateWeakDisposeAction(WeakReference<IDisposable> disposable)
        {
            return () => (disposable.TryGetTarget(out var target) ? target : null)?.Dispose();
        }

        public void Dispose()
        {
            lock (this._cleanupActions)
            {
                while (this._cleanupActions.Count > 0)
                {
                    this._cleanupActions.Pop()();
                }
                this.disposed = true;
            }
        }
    }
}
