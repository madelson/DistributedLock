using Medallion.Threading.Internal;
using Medallion.Threading.Tests.Data;
using NUnit.Framework;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;

namespace Medallion.Threading.Tests.Data
{
    public abstract class ConnectionStringStrategyTestCases<TLockProvider, TStrategy, TDb>
        where TLockProvider : TestingLockProvider<TStrategy>, new()
        where TStrategy : TestingOwnedConnectionSynchronizationStrategy<TDb>, new()
        // since we're just going to be generating from connection strings, we only care about
        // the primary ADO client for the database
        where TDb : ITestingPrimaryClientDb, new()
    {
        private TLockProvider _lockProvider = default!;

        [SetUp] public void SetUp() => this._lockProvider = new TLockProvider();
        [TearDown] public void TearDown() => this._lockProvider.Dispose();

        /// <summary>
        /// Tests that internally-owned connections are properly cleaned up by disposing the lock handle 
        /// </summary>
        [Test]
        public void TestConnectionDoesNotLeak()
        {
            // If the lock is based on a multi-ticket semaphore, then the first creation will claim N-1 connections. To avoid this messing with
            // our count, we create a throwaway lock instance here to hold those connections using the default application name
            this._lockProvider.CreateLock(nameof(TestConnectionDoesNotLeak));

            // set a distinctive application name so that we can count how many connections are used
            var applicationName = DistributedLockHelpers.ToSafeName(
                nameof(TestConnectionDoesNotLeak) + "_" + this.GetType(), 
                maxNameLength: this._lockProvider.Strategy.Db.MaxApplicationNameLength, s => s
            );
            this._lockProvider.Strategy.Db.ConnectionStringBuilder["Application Name"] = applicationName;

            var @lock = this._lockProvider.CreateLock(nameof(TestConnectionDoesNotLeak));
            for (var i = 0; i < 30; ++i)
            {
                using (@lock.Acquire())
                {
                    this._lockProvider.Strategy.Db.CountActiveSessions(applicationName).ShouldEqual(1, this.GetType().Name);
                }
                // still alive due to pooling
                this._lockProvider.Strategy.Db.CountActiveSessions(applicationName).ShouldEqual(1, this.GetType().Name);
            }

            using (var connection = this._lockProvider.Strategy.Db.CreateConnection())
            {
                this._lockProvider.Strategy.Db.ClearPool(connection);
            }

            // checking immediately seems flaky; likely clear pool finishing
            // doesn't guarantee that SQL will immediately reflect the clear
            var maxWaitForPoolsToClear = TimeSpan.FromSeconds(5);
            var stopwatch = Stopwatch.StartNew();
            do
            {
                var activeCount = this._lockProvider.Strategy.Db.CountActiveSessions(applicationName);
                if (activeCount == 0) { return; }
                Thread.Sleep(10);
            }
            while (stopwatch.Elapsed < maxWaitForPoolsToClear);

            Assert.Fail("Connection was not released");
        }
    }
}
