using Medallion.Threading.Tests.Sql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    public abstract class ConnectionStringStrategyTestCases<TEngineFactory, TConnectionManagementProvider> : TestBase
        where TEngineFactory : ITestingSqlDistributedLockEngineFactory, new()
        where TConnectionManagementProvider : ConnectionStringProvider, new()
    {
        /// <summary>
        /// Tests that internally-owned connections are properly cleaned up by disposing the lock handle 
        /// </summary>
        [Test]
        public void TestConnectionDoesNotLeak()
        {
            var applicationName = nameof(TestConnectionDoesNotLeak) + Guid.NewGuid();
            var connectionString = new SqlConnectionStringBuilder(ConnectionStringProvider.ConnectionString)
                {
                    ApplicationName = applicationName,
                }
                .ConnectionString;

            using (ConnectionStringProvider.UseConnectionString(connectionString))
            using (var engine = this.CreateEngine())
            {
                var @lock = engine.CreateLock(nameof(TestConnectionDoesNotLeak));

                for (var i = 0; i < 30; ++i)
                {
                    using (@lock.Acquire())
                    {
                        CountActiveSessions().ShouldEqual(1, this.GetType().Name);
                    }
                    // still alive due to pooling
                    CountActiveSessions().ShouldEqual(1, this.GetType().Name);
                }
            }

            using (var connection = new SqlConnection(connectionString))
            {
                SqlConnection.ClearPool(connection);
                // checking immediately seems flaky; likely clear pool finishing
                // doesn't guarantee that SQL will immediately reflect the clear
                var maxWaitForPoolsToClear = TimeSpan.FromSeconds(5);
                var stopwatch = Stopwatch.StartNew();
                do
                {
                    var activeCount = CountActiveSessions();
                    if (activeCount == 0) { return; }
                    Thread.Sleep(25);
                }
                while (stopwatch.Elapsed < maxWaitForPoolsToClear);
            }

            int CountActiveSessions()
            {
                using (var connection = new SqlConnection(ConnectionStringProvider.ConnectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $@"SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE program_name = '{applicationName}'";
                        return (int)command.ExecuteScalar();
                    }
                }
            }
        }

        private TestingDistributedLockEngine CreateEngine() => new TEngineFactory().Create<TConnectionManagementProvider>();
    }
}
