using Medallion.Threading.Sql;
using Medallion.Threading.Tests.Sql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    public abstract class ExternalConnectionOrTransactionStrategyTestCases<TEngineFactory, TConnectionManagementProvider>
        where TEngineFactory : ITestingSqlDistributedLockEngineFactory, new()
        where TConnectionManagementProvider : TestingSqlConnectionManagementProvider, IExternalConnectionOrTransactionTestingSqlConnectionManagementProvider, new()
    {
        [Test]
        public void TestDeadlockDetection()
        {
            var timeout = TimeSpan.FromSeconds(30);

            using (var engine = this.CreateEngine())
            using (var provider = new TConnectionManagementProvider())
            using (var barrier = new Barrier(participantCount: 2))
            {
                const string LockName1 = nameof(TestDeadlockDetection) + "_1",
                    LockName2 = nameof(TestDeadlockDetection) + "_2";

                Task RunDeadlock(bool isFirst)
                {
                    var connectionInfo = provider.GetConnectionInfo();
                    IDistributedLock lock1, lock2;
                    using (connectionInfo.Transaction != null ? TransactionProvider.UseTransaction(connectionInfo.Transaction) : ConnectionProvider.UseConnection(connectionInfo.Connection!))
                    {
                        lock1 = engine.CreateLock(isFirst ? LockName1 : LockName2);
                        lock2 = engine.CreateLock(isFirst ? LockName2 : LockName1);
                    }
                    return Task.Run(async () =>
                    {
                        using (await lock1.AcquireAsync(timeout))
                        {
                            barrier.SignalAndWait();
                            (await lock2.AcquireAsync(timeout)).Dispose();
                        }
                    });
                }

                var tasks = new[] { RunDeadlock(isFirst: true), RunDeadlock(isFirst: false) };

                Task.WhenAll(tasks).ContinueWith(_ => { }).Wait(TimeSpan.FromSeconds(10)).ShouldEqual(true, this.GetType().Name);

                var deadlockVictim = tasks.Single(t => t.IsFaulted);
                Assert.IsInstanceOf<InvalidOperationException>(deadlockVictim.Exception.GetBaseException()); // backwards compat check
                Assert.IsInstanceOf<DeadlockException>(deadlockVictim.Exception.GetBaseException());

                tasks.Count(t => t.Status == TaskStatus.RanToCompletion).ShouldEqual(1);
            }
        }

        private TestingDistributedLockEngine CreateEngine() => new TEngineFactory().Create<TConnectionManagementProvider>();
    }
}
