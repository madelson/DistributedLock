using NUnit.Framework;

namespace Medallion.Threading.Tests.Core;

[Category("CI")]
public class InternalVisibilityTest
{
    [Test]
    public void TestInternalNamespaceMethodsHaveCorrectVisibility()
    {
        var internalNamespaceTypes = typeof(IDistributedLock).Assembly.GetTypes()
            .Where(t => t.Namespace?.Contains(".Internal") ?? false)
            .ToList();
        Assert.That(internalNamespaceTypes, Is.Not.Empty);

#if !DEBUG
        Assert.That(internalNamespaceTypes.Where(t => t.IsPublic), Is.Empty);
#endif
    }
}
