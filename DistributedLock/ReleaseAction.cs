using System;
using System.Threading;

namespace Medallion.Threading
{
    internal sealed class ReleaseAction : IDisposable
    {
        private Action action;

        public ReleaseAction(Action action)
        {
            if (action == null) { throw new ArgumentNullException(nameof(action)); }

            this.action = action;
        }

        public void Dispose() => Interlocked.Exchange(ref this.action, null)?.Invoke();
    }
}
