using NUnit.Framework;
using System.Linq;

namespace Medallion.Threading.Tests.Core
{
    [Category("CI")]
    public class InternalVisibilityTest
    {
        [Test]
        public void TestInternalNamespaceMethodsHaveCorrectVisibility()
        {
            var internalNamespaceTypes = typeof(IDistributedLock).Assembly.GetTypes()
                .Where(t => t.Namespace?.Contains(".Internal") ?? false)
                .ToList();
            Assert.IsNotEmpty(internalNamespaceTypes);

#if !DEBUG
            Assert.IsEmpty(internalNamespaceTypes.Where(t => t.IsPublic));
#endif
        }
    }
}
