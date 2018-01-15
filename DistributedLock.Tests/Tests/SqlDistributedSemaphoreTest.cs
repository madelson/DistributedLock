using Medallion.Threading.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    [TestClass]
    public sealed class SqlDistributedSemaphoreTest : TestBase
    {
        [TestMethod]
        public void TestBadConstructorArguments()
        {
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedSemaphore(null, 1, ConnectionStringProvider.ConnectionString));
            TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => new SqlDistributedSemaphore("a", -1, ConnectionStringProvider.ConnectionString));
            TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => new SqlDistributedSemaphore("a", 0, ConnectionStringProvider.ConnectionString));
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedSemaphore("a", 1, default(string)));
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedSemaphore("a", 1, default(IDbConnection)));
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedSemaphore("a", 1, default(IDbTransaction)));
            TestHelper.AssertThrows<ArgumentException>(() => new SqlDistributedSemaphore("a", 1, ConnectionStringProvider.ConnectionString, (SqlDistributedLockConnectionStrategy)int.MinValue));

            var random = new Random(1234);
            var bytes = new byte[10000];
            random.NextBytes(bytes);
            TestHelper.AssertDoesNotThrow(() => new SqlDistributedSemaphore(Encoding.UTF8.GetString(bytes), int.MaxValue, ConnectionStringProvider.ConnectionString));
        }

        // todo add compat tests for name mangling
    }
}
