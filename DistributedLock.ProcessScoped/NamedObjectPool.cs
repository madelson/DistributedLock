using Medallion.Threading.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Medallion.Threading
{
    /// <summary>
    /// Manages a unique instance of an object of type <typeparamref name="T"/> referred to by a name.
    /// Callers can acquire a reference to the instance via the <see cref="LeaseObject(string)"/> method.
    /// When all leases are relinquished, the pooled instance becomes eligible for garbage collection.
    /// </summary>
    internal sealed class NamedObjectPool<T> where T : class
    {
        private readonly Dictionary<string, State> _instances = new();

        private readonly Func<string, T> _factory;

        public NamedObjectPool(Func<string, T> factory)
        {
            this._factory = factory;
        }

        private object Lock => this._instances;

        public ILease LeaseObject(string name) => Lease.Acquire(this, name);

        private sealed record State
        {
            public State(T instance) { this.Instance = instance; }

            public T Instance { get; }
            public ulong LeaseCount { get; set; }
        }

        public interface ILease : IDisposable
        {
            T Value { get; }
        }

        private sealed class Lease : ILease
        {
            private T? _value;
            private NamedObjectPool<T> _pool;
            private string _name;

            private Lease(NamedObjectPool<T> pool, string name)
            {
                this._pool = pool;
                this._name = name;
            }

            public T Value => this._value ?? throw new ObjectDisposedException(this.GetType().ToString());

            public static Lease Acquire(NamedObjectPool<T> pool, string name)
            {
                Lease lease = new(pool, name);
                lock (pool.Lock)
                {
                    if (!pool._instances.TryGetValue(name, out var state))
                    {
                        pool._instances.Add(name, state = new(pool._factory(name)));
                    }
                    checked { ++state.LeaseCount; }
                    lease._value = state.Instance;
                }
                return lease;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref this._value, null) is { } value)
                {
                    lock (this._pool.Lock)
                    {
                        var state = this._pool._instances[this._name];
                        Invariant.Require(ReferenceEquals(state.Instance, value));
                        checked { --state.LeaseCount; }
                        if (state.LeaseCount == 0)
                        {
                            this._pool._instances.Remove(this._name);
                        }
                    }
                    this._pool = null!;
                    this._name = null!;
                }
            }
        }
    }
}
