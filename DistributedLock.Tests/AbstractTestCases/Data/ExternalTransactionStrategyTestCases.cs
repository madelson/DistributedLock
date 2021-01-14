using NUnit.Framework;
using System;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Medallion.Threading.Tests.Data
{
    public abstract class ExternalTransactionStrategyTestCases<TLockProvider, TDb>
        where TLockProvider : TestingLockProvider<TestingExternalTransactionSynchronizationStrategy<TDb>>, new()
        where TDb : ITestingDb, new()
    {
        private TLockProvider _lockProvider = default!;

        [SetUp] public void SetUp() => this._lockProvider = new TLockProvider();
        [TearDown] public void TearDown() => this._lockProvider.Dispose();

        [Test]
        public void TestScopedToTransactionOnly()
        {
            this._lockProvider.Strategy.StartAmbient();

            var ambientTransactionLock = this._lockProvider.CreateLock(nameof(TestScopedToTransactionOnly));
            using (ambientTransactionLock.Acquire())
            {
                Assert.IsTrue(this._lockProvider.CreateLock(nameof(TestScopedToTransactionOnly)).IsHeld());

                // create a lock of the same type on the underlying connection of the ambient transaction
                using dynamic specificConnectionProvider = Activator.CreateInstance(
                    ReplaceGenericParameter(typeof(TLockProvider), this._lockProvider.Strategy.GetType(), typeof(SpecificConnectionStrategy))
                )!;
                specificConnectionProvider.Strategy.Test = this;
                Assert.Catch<InvalidOperationException>(() => ((IDistributedLock)specificConnectionProvider.CreateLock(nameof(TestScopedToTransactionOnly))).Acquire());
            }

            static Type ReplaceGenericParameter(Type type, Type old, Type @new)
            {
                if (type == old) { return @new; }
                if (!type.IsConstructedGenericType) { return type; }

                var newGenericArguments = type.GetGenericArguments()
                    .Select(a => ReplaceGenericParameter(a, old, @new))
                    .ToArray();
                return type.GetGenericTypeDefinition()
                    .MakeGenericType(newGenericArguments);
            }
        }

        /// <summary>
        /// Special strategy designed to allow us to make connection-scoped locks using the same connection as
        /// the ambient transaction from our own <see cref="_lockProvider"/>
        /// </summary>
        private class SpecificConnectionStrategy : TestingDbSynchronizationStrategy<TDb>
        {
            public ExternalTransactionStrategyTestCases<TLockProvider, TDb>? Test { get; set; }

            public override TestingDbConnectionOptions GetConnectionOptions() =>
                new TestingDbConnectionOptions { Connection = this.Test!._lockProvider.Strategy.AmbientTransaction!.Connection };
        }

        public void TestCloseTransactionLockOnClosedConnectionOrTransaction([Values] bool closeConnection)
        {
            var lockName = closeConnection ? "Connection" : "Transaction";

            var nonAmbientTransactionLock = this._lockProvider.CreateLock(lockName);

            // Disable pooling for the ambient connection. This is important because we want to show that the lock
            // will get released; in reality for a pooled connection in this scenario the lock-holding connection will
            // return to the pool and would get released the next time that connection was fetched from the pool
            this._lockProvider.Strategy.Db.ConnectionStringBuilder["Pooling"] = false;
            this._lockProvider.Strategy.StartAmbient();
            var ambientTransactionLock = this._lockProvider.CreateLock(lockName);

            using var handle = ambientTransactionLock.Acquire();
            Assert.IsTrue(nonAmbientTransactionLock.IsHeld());

            if (closeConnection)
            {
                this._lockProvider.Strategy.AmbientTransaction!.Connection.Dispose();
            }
            else
            {
                this._lockProvider.Strategy.AmbientTransaction!.Dispose();
            }
            Assert.DoesNotThrow(handle.Dispose);

            // now lock can be re-acquired
            Assert.IsFalse(nonAmbientTransactionLock.IsHeld());
        }

        [Test]
        public void TestLockOnRolledBackTransaction() => this.TestLockOnCompletedTransactionHelper(t => t.Rollback());

        [Test]
        public void TestLockOnCommittedTransaction() => this.TestLockOnCompletedTransactionHelper(t => t.Commit());

        [Test]
        public void TestLockOnDisposedTransaction() => this.TestLockOnCompletedTransactionHelper(t => t.Dispose());

        private void TestLockOnCompletedTransactionHelper(Action<DbTransaction> complete, [CallerMemberName] string lockName = "")
        {
            var nonAmbientTransactionLock = this._lockProvider.CreateLock(lockName);

            // Disable pooling for the ambient connection. This is important because we want to show that the lock
            // will get released; in reality for a pooled connection in this scenario the lock-holding connection will
            // return to the pool and would get released the next time that connection was fetched from the pool
            this._lockProvider.Strategy.Db.ConnectionStringBuilder["Pooling"] = false;
            this._lockProvider.Strategy.StartAmbient();
            var ambientTransactionLock = this._lockProvider.CreateLock(lockName);

            using var handle = ambientTransactionLock.Acquire();
            Assert.IsTrue(nonAmbientTransactionLock.IsHeld());

            complete(this._lockProvider.Strategy.AmbientTransaction!);

            Assert.DoesNotThrow(handle.Dispose);

            // now lock can be re-acquired
            Assert.IsFalse(nonAmbientTransactionLock.IsHeld());

            if (this._lockProvider.Strategy.Db.SupportsTransactionScopedSynchronization)
            {
                Assert.Catch<InvalidOperationException>(() => ambientTransactionLock.Acquire());
            }
            else
            {
                Assert.DoesNotThrow(() => ambientTransactionLock.Acquire().Dispose());
            }
        }
    }
}
