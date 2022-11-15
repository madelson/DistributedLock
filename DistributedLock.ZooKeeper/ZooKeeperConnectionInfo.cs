using Medallion.Threading.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Medallion.Threading.ZooKeeper;

internal sealed record ZooKeeperAuthInfo(string Scheme, EquatableReadOnlyList<byte> Auth);

internal sealed record ZooKeeperConnectionInfo(string ConnectionString, TimeoutValue ConnectTimeout, TimeoutValue SessionTimeout, EquatableReadOnlyList<ZooKeeperAuthInfo> AuthInfo);

internal readonly struct EquatableReadOnlyList<T> : IReadOnlyList<T>, IEquatable<EquatableReadOnlyList<T>>
{
    private readonly T[] _array;

    public EquatableReadOnlyList(IEnumerable<T> items)
    {
        this._array = items.ToArray();
    }

    public T this[int index] => this._array[index];

    public int Count => this._array.Length;

    public bool Equals(EquatableReadOnlyList<T> other) => this._array.SequenceEqual(other._array);

    public override bool Equals(object obj) => obj is EquatableReadOnlyList<T> that && this.Equals(that);

    public override int GetHashCode()
    {
        var hash = 0;
        foreach (var item in this._array)
        {
            hash = (hash, item).GetHashCode();
        }
        return hash;
    }

    public IEnumerator<T> GetEnumerator() => this._array.As<IEnumerable<T>>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
