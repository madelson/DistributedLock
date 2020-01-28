using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DistributedLock.Tests.Infrastructure
{
    internal sealed class Current<T> : IDisposable
        where T : class
    {
        private static readonly AsyncLocal<Current<T>> AsyncLocal = new AsyncLocal<Current<T>>();

        private volatile T? _value;

        private Current(T value) { this._value = value; }

        public static T? Value => AsyncLocal.Value?._value;

        public static IDisposable Use(T value)
        {
            if (Value != null) { throw new InvalidOperationException("already set"); }
            return AsyncLocal.Value = new Current<T>(value);
        }

        public void Dispose() => this._value = null;
    }
}
