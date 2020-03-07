using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Medallion.Threading.Internal
{
    // todo use in more places or get rid of
#if DEBUG
    public
#else
    internal
#endif
    sealed class RefBox<T> where T : struct
    {
        private readonly T _value;

        internal RefBox(T value)
        {
            this._value = value;
        }

        public ref readonly T Value => ref this._value;
    }

#if DEBUG
    public
#else
    internal
#endif
    sealed class RefBox
    {
        public static RefBox<T> Create<T>(T value) where T : struct => new RefBox<T>(value);

        public static bool TryConsume<T>(ref RefBox<T>? boxRef, out T value)
            where T : struct
        {
            var box = Interlocked.Exchange(ref boxRef, null);
            if (box != null)
            {
                value = box.Value;
                return true;
            }

            value = default;
            return false;
        }
    }
}
