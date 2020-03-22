using Medallion.Threading.Internal;
using NUnit.Framework;
using System.Data;

namespace Medallion.Threading.Tests.Data
{
    public abstract class OwnedTransactionStrategyTestCases<TLockProvider, TDb>
        where TLockProvider : TestingLockProvider<TestingOwnedTransactionSynchronizationStrategy<TDb>>, new()
        where TDb : ITestingPrimaryClientDb, new()
    {
        private TLockProvider _lockProvider = default!;

        [SetUp] public void SetUp() => this._lockProvider = new TLockProvider();
        [TearDown] public void TearDown() => this._lockProvider.Dispose();

        /// <summary>
        /// Validates that we use the default isolation level to avoid the problem described
        /// here: https://msdn.microsoft.com/en-us/library/5ha4240h(v=vs.110).aspx
        /// 
        /// From MSDN:
        /// After a transaction is committed or rolled back, the isolation level of the transaction 
        /// persists for all subsequent commands that are in autocommit mode (the SQL Server default). 
        /// This can produce unexpected results, such as an isolation level of REPEATABLE READ persisting 
        /// and locking other users out of a row. To reset the isolation level to the default (READ COMMITTED), 
        /// execute the Transact-SQL SET TRANSACTION ISOLATION LEVEL READ COMMITTED statement, or call 
        /// SqlConnection.BeginTransaction followed immediately by SqlTransaction.Commit. For more 
        /// information on SQL Server isolation levels, see "Isolation Levels in the Database Engine" in SQL 
        /// Server Books Online.
        /// 
        /// This obviously only applies to SQLServer currently. However, we might as well run this test against
        /// other providers in case they have the same issue.
        /// </summary>
        [Test]
        public void TestIsolationLevelLeakage()
        {
            // Pre-generate the lock we will use. This is necessary for our Semaphore5 strategy, where the first lock created
            // takes 4 of the 5 tickets (and thus may need more connections than a single-connection pool can support). For other
            // lock types this does nothing since creating a lock might open a connection but otherwise won't run any commands
            this._lockProvider.CreateLock(nameof(TestIsolationLevelLeakage));

            // use a unique pool of size 1 so we can reclaim the connection after we use it and test for leaks
            this._lockProvider.Strategy.Db.ConnectionStringBuilder["Application Name"] = DistributedLockHelpers.ToSafeName(
                this._lockProvider.GetUniqueSafeName(),
                maxNameLength: this._lockProvider.Strategy.Db.MaxApplicationNameLength, s => s
            );
            this._lockProvider.Strategy.Db.MaxPoolSize = 1;

            AssertHasDefaultIsolationLevel();

            var @lock = this._lockProvider.CreateLock(nameof(TestIsolationLevelLeakage));
            @lock.Acquire().Dispose();
            AssertHasDefaultIsolationLevel();

            @lock.AcquireAsync().Result.Dispose();
            AssertHasDefaultIsolationLevel();

            void AssertHasDefaultIsolationLevel()
            {
                using var connection = this._lockProvider.Strategy.Db.CreateConnection();
                connection.Open();
                this._lockProvider.Strategy.Db.GetIsolationLevel(connection).ShouldEqual(IsolationLevel.ReadCommitted);
            }
        }
    }
}
