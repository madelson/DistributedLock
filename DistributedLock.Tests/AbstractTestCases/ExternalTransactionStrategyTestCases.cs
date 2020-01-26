using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    public abstract class ExternalTransactionStrategyTestCases<TEngineFactory> : TestBase
        where TEngineFactory : ITestingSqlDistributedLockEngineFactory, new()
    {
        [Test]
        public void TestScopedToTransactionOnly()
        {
            using (var connection = new SqlConnection(ConnectionStringProvider.ConnectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                using (TransactionProvider.UseTransaction(transaction))
                using (var transactionEngine = new TEngineFactory().Create<TransactionProvider>())
                {
                    var lockName = nameof(TestScopedToTransactionOnly);
                    using (transactionEngine.CreateLock(lockName).Acquire())
                    {
                        using (var handle = transactionEngine.CreateLock(lockName).TryAcquire())
                        {
                            (handle != null).ShouldEqual(transactionEngine.IsReentrant, "reentrant: " + this.GetType().Name);
                        }

                        using (ConnectionProvider.UseConnection(connection))
                        using (var connectionEngine = new TEngineFactory().Create<ConnectionProvider>())
                        {
                            Assert.Catch<InvalidOperationException>(() => connectionEngine.CreateLock(lockName).TryAcquire());
                        }
                    }
                }
            }
        }

        [Test]
        public void TestCloseTransactionLockOnClosedConnection()
        {
            using (var connection = new SqlConnection(ConnectionStringProvider.ConnectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                using (TransactionProvider.UseTransaction(transaction))
                using (var transactionEngine = new TEngineFactory().Create<TransactionProvider>())
                using (var connectionStringEngine = new TEngineFactory().Create<DefaultConnectionStringProvider>())
                {
                    var lockName = nameof(TestCloseTransactionLockOnClosedConnection);
                    var @lock = transactionEngine.CreateLock(lockName);
                    var handle = @lock.Acquire();
                    // use connectionStringEngine to avoid reentrance
                    connectionStringEngine.CreateLock(lockName).IsHeld().ShouldEqual(true);

                    connection.Dispose();

                    Assert.DoesNotThrow(handle.Dispose);

                    // lock can be re-acquired
                    connectionStringEngine.CreateLock(lockName).IsHeld().ShouldEqual(false);
                }
            }
        }

        [Test]
        public void TestCloseTransactionLockOnClosedTransaction()
        {
            using (var connectionStringEngine = new TEngineFactory().Create<DefaultConnectionStringProvider>())
            using (var connection = new SqlConnection(ConnectionStringProvider.ConnectionString))
            {
                connection.Open();

                var lockName = nameof(TestCloseTransactionLockOnClosedTransaction);

                IDisposable handle;
                using (var transaction = connection.BeginTransaction())
                using (TransactionProvider.UseTransaction(transaction))
                using (var transactionEngine = new TEngineFactory().Create<TransactionProvider>())
                {
                    handle = transactionEngine.CreateLock(lockName).Acquire();
                    connectionStringEngine.CreateLock(lockName).IsHeld().ShouldEqual(true);
                }
                Assert.DoesNotThrow(handle.Dispose);
                connectionStringEngine.CreateLock(lockName).IsHeld().ShouldEqual(false);
            }
        }

        [Test]
        public void TestLockOnRolledBackTransaction() => this.TestLockOnCompletedTransactionHelper(t => t.Rollback());

        [Test]
        public void TestLockOnCommittedTransaction() => this.TestLockOnCompletedTransactionHelper(t => t.Commit());

        [Test]
        public void TestLockOnDisposedTransaction() => this.TestLockOnCompletedTransactionHelper(t => t.Dispose());

        private void TestLockOnCompletedTransactionHelper(Action<SqlTransaction> complete, [CallerMemberName] string lockName = "")
        {
            using (var connectionStringEngine = new TEngineFactory().Create<DefaultConnectionStringProvider>())
            using (var connection = new SqlConnection(ConnectionStringProvider.ConnectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                using (TransactionProvider.UseTransaction(transaction))
                using (var transactionEngine = new TEngineFactory().Create<TransactionProvider>())
                {
                    var handle = transactionEngine.CreateLock(lockName).Acquire();
                    connectionStringEngine.CreateLock(lockName).IsHeld().ShouldEqual(true);

                    complete(transaction);

                    Assert.DoesNotThrow(handle.Dispose);
                    connectionStringEngine.CreateLock(lockName).IsHeld().ShouldEqual(false, this.GetType().Name);

                    Assert.Catch<InvalidOperationException>(() => transactionEngine.CreateLock(lockName).Acquire());
                }
            }
        }
    }
}
