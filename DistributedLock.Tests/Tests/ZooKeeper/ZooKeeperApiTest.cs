using Medallion.Threading.ZooKeeper;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Medallion.Threading.Tests.ZooKeeper;

[Category("CI")]
public class ZooKeeperApiTest
{
    [Test]
    public void TestSynchronousAcquireAndDisposeMethodsAreImplementedExplicitly()
    {
        Assert.IsEmpty(
            GetPublicTypes().SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Where(m => m.Name == "Dispose" || (m.Name.Contains("Acquire") && !m.Name.EndsWith("Async")))
        );
    }

    [Test]
    public void TestNamePropertyIsImplementedExplicitlyInFavorOfPath()
    {
        Assert.IsEmpty(GetPublicTypes().Select(t => t.GetProperty("Name")).Where(p => p != null));
        foreach (var lockType in GetPublicTypes()
            .Where(t => t.GetInterfaces().Any(i => i.GetProperty("Name") != null)))
        {
            var pathProperty = lockType.GetProperty("Path");
            Assert.IsNotNull(pathProperty, $"{lockType} missing Path");
            pathProperty!.PropertyType.ShouldEqual(typeof(ZooKeeperPath));
            Assert.IsNull(lockType.GetProperty("Name"));
        }
    }

    private static IEnumerable<Type> GetPublicTypes() => typeof(ZooKeeperDistributedLock).Assembly.GetTypes()
        .Where(t => t.IsPublic || t.IsNestedPublic);
}
