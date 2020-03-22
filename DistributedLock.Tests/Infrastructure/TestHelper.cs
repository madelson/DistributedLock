using NUnit.Framework;

namespace Medallion.Threading.Tests
{
    internal static class TestHelper
    {
        public static T ShouldEqual<T>(this T @this, T that, string? message = null)
        {
            Assert.AreEqual(actual: @this, expected: that, message: message);
            return @this;
        }

        public static bool IsHeld(this IDistributedLock @lock)
        {
            using var handle = @lock.TryAcquire();
            return handle == null;
        }
    }
}
