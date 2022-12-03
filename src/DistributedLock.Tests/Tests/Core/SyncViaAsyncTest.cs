using Medallion.Threading.Internal;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Core;

[Category("CI")]
public class SyncViaAsyncTest
{
    [Test]
    public void TestSyncOverAsyncVoid()
    {
        var currentThread = Thread.CurrentThread;
        SyncViaAsync.Run<(int a, int b, Thread startingThread, bool expectAsync)>(
            async state => await AddAsync(state.a, state.b, state.startingThread, state.expectAsync),
            (1, 2, currentThread, false)
        );
    }

    [Test]
    public void TestSyncOverAsyncWithResult()
    {
        var currentThread = Thread.CurrentThread;
        var result = SyncViaAsync.Run(
            async ((int a, int b, Thread startingThread, bool expectAsync) state) => await AddAsync(state.a, state.b, state.startingThread, state.expectAsync),
            (1, 2, currentThread, false)
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
        Assert.AreNotEqual(expectAsync, SyncViaAsync.IsSynchronous);

        if (expectAsync) { await Task.Delay(1); }
        else { Thread.Sleep(1); }

        return a;
    }
}
