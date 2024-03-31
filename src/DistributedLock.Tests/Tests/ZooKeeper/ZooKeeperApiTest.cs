using Medallion.Threading.ZooKeeper;
using NUnit.Framework;
using System.Reflection;

namespace Medallion.Threading.Tests.ZooKeeper;

[Category("CI")]
public class ZooKeeperApiTest
{
    [Test]
    public void TestSynchronousAcquireAndDisposeMethodsAreImplementedExplicitly()
    {
        Assert.That(
            GetPublicTypes().SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Where(m => m.Name == "Dispose" || (m.Name.Contains("Acquire") && !m.Name.EndsWith("Async")))
, Is.Empty);
    }

    [Test]
    public void TestNamePropertyIsImplementedExplicitlyInFavorOfPath()
    {
        Assert.That(GetPublicTypes().Select(t => t.GetProperty("Name")).Where(p => p != null), Is.Empty);
        foreach (var lockType in GetPublicTypes()
            .Where(t => t.GetInterfaces().Any(i => i.GetProperty("Name") != null)))
        {
            var pathProperty = lockType.GetProperty("Path");
            Assert.That(pathProperty, Is.Not.Null, $"{lockType} missing Path");
            pathProperty!.PropertyType.ShouldEqual(typeof(ZooKeeperPath));
            Assert.That(lockType.GetProperty("Name"), Is.Null);
        }
    }

    private static IEnumerable<Type> GetPublicTypes() => typeof(ZooKeeperDistributedLock).Assembly.GetTypes()
        .Where(t => t.IsPublic || t.IsNestedPublic);
}
