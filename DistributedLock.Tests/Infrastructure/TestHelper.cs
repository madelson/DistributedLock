using Medallion.Threading.Internal;
using Medallion.Threading.Redis;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    internal static class TestHelper
    {
        /// <summary>
        /// Returns a name that is unique to the current test and target framework but stable otherwise.
        /// </summary>
        public static string UniqueName => $"{TestContext.CurrentContext.Test.FullName}_{TargetFramework.Current}";

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

        public static async Task<bool> WaitAsync(this Task task, TimeoutValue timeout)
        {
            if (!task.IsCompleted)
            {
                using var timeoutTask = new TimeoutTask(timeout, CancellationToken.None);
                if (await Task.WhenAny(task, timeoutTask.Task) != task)
                {
                    return false;
                }
            }

            await task;
            return true;
        }
    }
}
