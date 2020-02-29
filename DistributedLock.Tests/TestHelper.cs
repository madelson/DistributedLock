using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    internal static class TestHelper
    {
        public static T ShouldEqual<T>(this T @this, T that, string? message = null)
        {
            Assert.AreEqual(actual: @this, expected: that, message: message);
            return @this;
        }

        public static bool IsHeld(this IDistributedLockOld @lock)
        {
            using var handle = @lock.TryAcquire();
            return handle == null;
        }

        public const string FrameworkName =
#if NET471
                "net471";
#elif NETCOREAPP3_1
                "netcoreapp3.1";
#endif
    }
}
