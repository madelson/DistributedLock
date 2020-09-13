using System;
using System.Collections.Generic;
using Medallion.Threading.Sql;
using Microsoft.Data.SqlClient;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Medallion.Threading.Tests.Sql
{
    public class SqlDistributedLocksTest
    {
        private const string ConnectionString = "Server=vm-pc-sql02;Database=NEU14270_200509_Seba;Trusted_Connection=True;";
        private readonly SqlConnection conn;

        public SqlDistributedLocksTest()
        {
            conn = new SqlConnection(ConnectionString);
            conn.Open();
        }

        [Test]
        public void TestBasicLocks()
        {
            using var tran = conn.BeginTransaction();
            var locks = new SqlDistributedLocks(new [] {"LOCK1", "LOCK2"}, tran);
            using var handle = locks.Acquire();
        }

        [Test]
        public void TestBasicLocksOwnedConnection()
        {
            using var tran = conn.BeginTransaction();
            var locks = new SqlDistributedLocks(new[] { "LOCK1", "LOCK2" }, ConnectionString);
            using var handle = locks.Acquire();
        }

        [Test]
        public void TestBasicLocksOwnedTransaction()
        {
            using var tran = conn.BeginTransaction();
            var locks = new SqlDistributedLocks(new[] { "LOCK1", "LOCK2" }, ConnectionString, SqlDistributedLockConnectionStrategy.Transaction);
            using var handle = locks.Acquire();
        }

        [Test]
        public void TestFailAcquiringLocks()
        {
            using var tran = conn.BeginTransaction();
            var locks = new SqlDistributedLocks(new[] { "LOCK1", "LOCK2" }, tran);
            using var handle = locks.Acquire();

            using var conn2 = new SqlConnection("Server=vm-pc-sql02;Database=NEU14270_200509_Seba;Trusted_Connection=True;");
            conn2.Open();
            using var tran2 = conn2.BeginTransaction();
            var locks2 = new SqlDistributedLocks(new[] { "LOCK1", "LOCK3" }, tran2);
            Assert.Throws<TimeoutException>(() =>
            {
                using var handle2 = locks2.Acquire(TimeSpan.Zero);
            });
            handle.Dispose();
            using var lockListCmd = new SqlCommand(@"
                SELECT l.*
                FROM sys.dm_tran_locks l
                WHERE resource_type = 'APPLICATION'", conn, tran);
            using var reader = lockListCmd.ExecuteReader();
            Assert.IsFalse(reader.HasRows, "There should be no locks");
        }

        [Test]
        public void TestHighVolumeLocks()
        {
            using var tran = conn.BeginTransaction();
            var lockNames = new List<string>();
            for (var i = 0; i < 500; i++)
                lockNames.Add($"LOCK{i}");
            var locks = new SqlDistributedLocks(lockNames, tran);
            using var handle = locks.Acquire();
        }
    }
}
