using DistributedLock.Tests;
using Medallion.Threading.Sql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    public abstract class OwnedTransactionStrategyTestCases<TEngineFactory>
        where TEngineFactory : ITestingSqlDistributedLockEngineFactory, new()
    {
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
        /// </summary>
        [Test]
        public void TestIsolationLevelLeakage()
        {
            const string IsolationLevelQuery = @"
                SELECT CASE transaction_isolation_level 
                WHEN 0 THEN 'Unspecified' 
                WHEN 1 THEN 'ReadUncommitted' 
                WHEN 2 THEN 'ReadCommitted' 
                WHEN 3 THEN 'RepeatableRead' 
                WHEN 4 THEN 'Serializable' 
                WHEN 5 THEN 'Snapshot' END AS isolationLevel 
                FROM sys.dm_exec_sessions 
                WHERE session_id = @@SPID";

            var connectionString = new SqlConnectionStringBuilder(ConnectionStringProvider.ConnectionString)
                {
                    ApplicationName = nameof(TestIsolationLevelLeakage),
                    // makes it easy to test for leaks since all connections are the same
                    MaxPoolSize = 1,
                }
                .ConnectionString;
            using (var connection = SqlHelpers.CreateConnection(connectionString)) { SqlTestHelper.ClearPool(connection); }

            using var engine = new TEngineFactory().Create<TransactionBasedConnectionStringProvider>();
            var @lock = engine.CreateLock(nameof(TestIsolationLevelLeakage));

            @lock.Acquire().Dispose();
            using (var connection = SqlHelpers.CreateConnection(connectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = IsolationLevelQuery;
                command.ExecuteScalar().ShouldEqual(IsolationLevel.ReadCommitted.ToString());
            }

            @lock.AcquireAsync().Result.Dispose();
            using (var connection = SqlHelpers.CreateConnection(connectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = IsolationLevelQuery;
                command.ExecuteScalar().ShouldEqual(IsolationLevel.ReadCommitted.ToString());
            }
        }
    }
}
