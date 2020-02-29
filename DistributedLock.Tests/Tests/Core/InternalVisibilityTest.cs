using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Tests.Core
{
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
            Assert.IsEmpty(internalNamespaceTypes.Where(t => !t.IsPublic));
#endif
        }
    }
}
