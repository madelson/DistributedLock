using NUnit.Framework;
using System;

namespace Medallion.Threading.Tests.Data
{
    public abstract class ExternalConnectionStrategyTestCases<TLockProvider, TDb>
        where TLockProvider : TestingLockProvider<TestingExternalConnectionSynchronizationStrategy<TDb>>, new()
        where TDb : TestingDb, new()
    {
        private TLockProvider _lockProvider = default!;

        [SetUp] public void SetUp() => this._lockProvider = new TLockProvider();
        [TearDown] public void TearDown() => this._lockProvider.Dispose();

        [Test]
        public void TestCloseLockOnClosedConnection()
        {
            var nonAmbientConnectionLock = this._lockProvider.CreateLock(nameof(TestCloseLockOnClosedConnection));

            // Disable pooling for the ambient connection. This is important because we want to show that the lock
            // will get released; in reality for a pooled connection in this scenario the lock-holding connection will
            // return to the pool and would get released the next time that connection was fetched from the pool
            this._lockProvider.Strategy.Db.ConnectionStringBuilder["Pooling"] = false;
            this._lockProvider.Strategy.StartAmbient();
            var ambientConnectionLock = this._lockProvider.CreateLock(nameof(TestCloseLockOnClosedConnection));

            this._lockProvider.Strategy.AmbientConnection!.Close();

            Assert.Catch<InvalidOperationException>(() => ambientConnectionLock.Acquire());

            this._lockProvider.Strategy.AmbientConnection!.Open();

            var handle = ambientConnectionLock.Acquire();
            
            nonAmbientConnectionLock.IsHeld().ShouldEqual(true, this.GetType().Name);

            this._lockProvider.Strategy.AmbientConnection!.Close();

            // Note: in version 1.0 we'd avoid throwing in this scenario. However, that approach could hide bugs because
            // merely closing the connection doesn't release the lock: it just returns the connection to the pool where
            // it will continue to hold the lock until it is used again.
            Assert.Throws<InvalidOperationException>(() => handle.Dispose());

            // lock can be re-acquired
            nonAmbientConnectionLock.IsHeld().ShouldEqual(false);
        }

        [Test]
        public void TestIsNotScopedToTransaction()
        {
            var nonAmbientConnectionLock = this._lockProvider.CreateLock(nameof(TestIsNotScopedToTransaction));

            this._lockProvider.Strategy.StartAmbient();

            using var handle = this._lockProvider.CreateLock(nameof(TestIsNotScopedToTransaction)).Acquire();
            using (var transaction = this._lockProvider.Strategy.AmbientConnection!.BeginTransaction())
            {
                transaction.Rollback();
            }

            nonAmbientConnectionLock.IsHeld().ShouldEqual(true, this.GetType().Name);
        }
    }
}
