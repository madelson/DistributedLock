using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Medallion.Threading
{
    internal sealed class ReleaseAction : IDisposable
    {
        private Action? action;

        public ReleaseAction(Action action)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public void Dispose() => Interlocked.Exchange(ref this.action, null)?.Invoke();
    }
}
