using Medallion.Threading.Sql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    public abstract class AzureConnectionStrategyTestCases<TEngineFactory> 
        where TEngineFactory : ITestingSqlDistributedLockEngineFactory, new()
    {
        [Test]
        public void TestIdleSessionKiller()
        {
            using (var engine = new TEngineFactory().Create<DefaultConnectionStringProvider>())
            using (var idleSessionKiller = new IdleSessionKiller(ConnectionStringProvider.ConnectionString, idleTimeout: TimeSpan.FromSeconds(.25)))
            {
                var @lock = engine.CreateLock(nameof(TestIdleSessionKiller));
                var handle = @lock.Acquire();
                Thread.Sleep(TimeSpan.FromSeconds(1));
                Assert.Catch<SqlException>(() => handle.Dispose());
            }
        }

        [Test]
        public void TestAzureStrategyProtectsFromIdleSessionKiller()
        {
            using (var engine = this.CreateEngine())
            {
                var originalInterval = KeepaliveHelper.Interval;
                try
                {
                    KeepaliveHelper.Interval = TimeSpan.FromSeconds(.1);

                    using (var idleSessionKiller = new IdleSessionKiller(ConnectionStringProvider.ConnectionString, idleTimeout: TimeSpan.FromSeconds(.25)))
                    {
                        var @lock = engine.CreateLock(nameof(TestAzureStrategyProtectsFromIdleSessionKiller));
                        var handle = @lock.Acquire();
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                        Assert.DoesNotThrow(() => handle.Dispose());
                    }
                }
                finally
                {
                    KeepaliveHelper.Interval = originalInterval;
                }
            }
        }

        /// <summary>
        /// Demonstrates that we don't multi-thread the connection despite the <see cref="KeepaliveHelper"/>
        /// </summary>
        [Test]
        public void ThreadSafetyExercise()
        {
            using (var engine = this.CreateEngine())
            {
                var originalInterval = KeepaliveHelper.Interval;
                try
                {
                    KeepaliveHelper.Interval = TimeSpan.FromMilliseconds(1);

                    Assert.DoesNotThrow(() =>
                    {
                        var @lock = engine.CreateLock(nameof(ThreadSafetyExercise));
                        for (var i = 0; i < 25; ++i)
                        {
                            using (@lock.Acquire())
                            {
                                Thread.Sleep(1);
                            }
                        }
                    });
                }
                finally
                {
                    KeepaliveHelper.Interval = originalInterval;
                }
            }
        }

        private TestingDistributedLockEngine CreateEngine() => new TEngineFactory().Create<AzureConnectionStringProvider>();
    }
}
