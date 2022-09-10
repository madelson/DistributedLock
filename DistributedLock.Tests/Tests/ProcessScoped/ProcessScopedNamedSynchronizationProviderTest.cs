using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.ProcessScoped
{
    public class ProcessScopedNamedSynchronizationProviderTest
    {
        [Test]
        public void TestCanCreatePrimitives()
        {
            ProcessScopedNamedSynchronizationProvider provider = new();
            provider.CreateLock("abc").Name.ShouldEqual("abc");
            provider.CreateReaderWriterLock("123").Name.ShouldEqual("123");
            Assert.IsTrue(provider.CreateSemaphore("x", 37) is { Name: "x", MaxCount: 37 });
        }
    }
}
