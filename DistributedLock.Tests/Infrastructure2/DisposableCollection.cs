using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Medallion.Threading.Tests
{
    internal sealed class DisposableCollection : IDisposable
    {
        private readonly object _lock = new object();
        private Stack<IDisposable>? _resources = new Stack<IDisposable>();

        public void Add(IDisposable resource)
        {
            lock (this._lock)
            {
                (this._resources ?? throw new ObjectDisposedException(this.GetType().ToString()))
                    .Push(resource);
            }
        }

        public void ClearAndDisposeAll() => this.InternalClearAndDisposeAll(isDispose: false);

        public void Dispose() => this.InternalClearAndDisposeAll(isDispose: true);

        private void InternalClearAndDisposeAll(bool isDispose)
        {
            lock (this._lock)
            {
                if (this._resources == null)
                {
                    if (isDispose) { return; }
                    throw new ObjectDisposedException(this.GetType().ToString());
                }

                var exceptions = new List<Exception>();
                while (this._resources.Count > 0)
                {
                    try { this._resources.Pop().Dispose(); }
                    catch (Exception ex) { exceptions.Add(ex); }
                }

                if (isDispose)
                {
                    this._resources = null;
                }

                if (exceptions.Any())
                {
                    throw new AggregateException(exceptions).Flatten();
                }
            }
        }
    }
}
