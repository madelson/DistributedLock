using Medallion.Threading.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Core;

[Category("CI")]
public class HelpersTest
{
    [Test]
    public void TestSafeCreateTaskPassesThroughSafeTasks()
    {
        var tasks = new[] { Task.FromResult(14), Task.FromResult(24) };
        var safeTask = Helpers.SafeCreateTask(state => tasks[state], 1);
        Assert.AreSame(tasks[1], safeTask);

        var safeNonGenericTask = Helpers.SafeCreateTask<int>(state => tasks[state], 1);
        Assert.AreSame(safeNonGenericTask, tasks[1]);
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
}
