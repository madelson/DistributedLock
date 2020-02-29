using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Tests.Core
{
    public class DistributedLockExtensionsTest
    {
        [Test]
        public void TestArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => DistributedLockProviderExtensions.TryAcquireAsync(null!, "name"));
            Assert.Throws<ArgumentNullException>(() => DistributedLockProviderExtensions.TryAcquire(null!, "name"));
            Assert.Throws<ArgumentNullException>(() => DistributedLockProviderExtensions.AcquireAsync(null!, "name"));
            Assert.Throws<ArgumentNullException>(() => DistributedLockProviderExtensions.Acquire(null!, "name"));
        }

        [Test, Combinatorial]
        public void TestCallThrough([Values] bool isTry, [Values] bool isAsync)
        {
            var mockLock = new Mock<IDistributedLock>();
            var mockProvider = new Mock<IDistributedLockProvider>();
            mockProvider.Setup(p => p.CreateLock("name", false))
                .Returns(mockLock.Object)
                .Verifiable();

            if (isTry)
            {
                if (isAsync)
                {
                    Test(p => p.TryAcquireAsync("name", default, default, false), l => l.TryAcquireAsync(default, default));
                }
                else
                {
                    Test(p => p.TryAcquire("name", default, default, false), l => l.TryAcquire(default, default));
                }
            }
            else
            {
                if (isAsync)
                {
                    Test(p => p.AcquireAsync("name", default, default, false), l => l.AcquireAsync(default, default));
                }
                else
                {
                    Test(p => p.Acquire("name", default, default, false), l => l.Acquire(default, default));
                }
            }

            void Test<TResult>(
                Expression<Func<IDistributedLockProvider, TResult>> providerFunction, 
                Expression<Func<IDistributedLock, TResult>> lockFunction)
            {
                providerFunction.Compile()(mockProvider.Object);

                mockProvider.Verify(p => p.CreateLock("name", false), Times.Once);
                mockLock.Verify(lockFunction, Times.Once());
            }
        }
    }
}
