using Medallion.Threading.Internal;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Medallion.Threading.Tests.Core;

[Category("CI")]
public class HelpersTest
{
    [Test]
    public void TestSafeCreateTaskPassesThroughSafeTasks()
    {
        var tasks = new[] { Task.FromResult(14), Task.FromResult(24) };
        var safeTask = Helpers.SafeCreateTask(state => tasks[state], 1);
        Assert.That(safeTask, Is.SameAs(tasks[1]));

        var safeNonGenericTask = Helpers.SafeCreateTask<int>(state => tasks[state], 1);
        Assert.That(tasks[1], Is.SameAs(safeNonGenericTask));
    }

    [Test]
    public void TestSafeCreateTaskReturnsCaughtExceptionAsFaultedTask()
    {
        var safeTask = Helpers.SafeCreateTask(state => GetTask(state), "m1");
        Assert.IsInstanceOf<TimeZoneNotFoundException>(safeTask.Exception!.InnerException);
        safeTask.Exception.InnerException!.Message.ShouldEqual("m1");

        var safeNonGenericTask = Helpers.SafeCreateTask<string>(state => GetTask(state), "m2");
        Assert.IsInstanceOf<TimeZoneNotFoundException>(safeNonGenericTask.Exception!.InnerException);
        safeNonGenericTask.Exception.InnerException!.Message.ShouldEqual("m2");

        static Task<string> GetTask(string message) => throw new TimeZoneNotFoundException(message);
    }

    /// <summary>
    /// Based on https://github.com/madelson/DistributedLock/issues/192
    /// </summary>
    [Test]
    public async Task TestTryAwaitShouldNotResultInUnobservedTaskException([Values] bool faulted)
    {
        AsyncLocal<bool> scope = new() { Value = true };
        ConcurrentBag<Exception> unobservedTaskExceptions = [];
        EventHandler<UnobservedTaskExceptionEventArgs> handler = (_, e) => unobservedTaskExceptions.Add(e.Exception);

        TaskScheduler.UnobservedTaskException += handler;
        try
        {
            await TryAwaitFailedTask();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        finally { TaskScheduler.UnobservedTaskException -= handler; }

        foreach (var exception in unobservedTaskExceptions)
        {
            Assert.That(exception.ToString(), Does.Not.Contain(nameof(TimeZoneNotFoundException)));
            Assert.That(exception.ToString(), Does.Not.Contain(nameof(OperationCanceledException)));
            Assert.That(exception.ToString(), Does.Not.Contain(nameof(TaskCanceledException)));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        async Task TryAwaitFailedTask()
        {
            Task task;
            if (faulted)
            {
                task = Task.Run(() => throw new TimeZoneNotFoundException());
            }
            else
            {
                CancellationTokenSource cancellationSource = new();
                task = Task.Delay(TimeSpan.FromSeconds(30), cancellationSource.Token);
                cancellationSource.Cancel();
            }

            await task.TryAwait();
        }
    }
}
