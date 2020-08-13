using Medallion.Threading.Internal;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Core
{
    [Category("CI")]
    public class SyncOverAsyncTest
    {
        [Test]
        public void TestSyncOverAsyncVoid([Values] bool willGoAsync)
        {
            var currentThread = Thread.CurrentThread;
            SyncOverAsync.Run<(int a, int b, Thread startingThread, bool expectAsync)>(
                async state => await AddAsync(state.a, state.b, state.startingThread, state.expectAsync),
                (1, 2, currentThread, willGoAsync),
                willGoAsync: willGoAsync
            );
        }

        [Test]
        public void TestSyncOverAsyncWithResult([Values] bool willGoAsync)
        {
            var currentThread = Thread.CurrentThread;
            var result = SyncOverAsync.Run(
                async ((int a, int b, Thread startingThread, bool expectAsync) state) => await AddAsync(state.a, state.b, state.startingThread, state.expectAsync),
                (1, 2, currentThread, willGoAsync),
                willGoAsync: willGoAsync
            );
            Assert.AreEqual(3, result);
        }

        private async ValueTask<int> AddAsync(int a, int b, Thread startingThread, bool expectAsync)
        {
            var result = await AddHelperAsync(a, expectAsync) + await AddHelperAsync(b, expectAsync);
            Assert.AreEqual(expectAsync, Thread.CurrentThread != startingThread);
            return result;
        }

        private async ValueTask<int> AddHelperAsync(int a, bool expectAsync)
        {
            Assert.AreNotEqual(expectAsync, SyncOverAsync.IsSynchronous);

            if (expectAsync) { await Task.Delay(1); }
            else { Thread.Sleep(1); }

            return a;
        }
    }
}
