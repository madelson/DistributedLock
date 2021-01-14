using System;
using System.Diagnostics;

namespace Medallion.Threading.Internal
{
#if DEBUG
    public
#else
    internal
#endif
    static class Invariant
    {
        [Conditional("DEBUG")]
        public static void Require(bool condition, string? message = null)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message ?? "invariant violated");
            }
        }
    }
}
