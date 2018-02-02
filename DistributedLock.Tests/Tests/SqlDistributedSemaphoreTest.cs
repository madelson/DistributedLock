using Medallion.Threading.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        [TestMethod]
        public void TestNameMangling()
        {
            string ToSafeNameChecked(string name)
            {
                var safeName = SqlSemaphore.ToSafeName(name);
                (safeName.Length > 0).ShouldEqual(true, "was: " + safeName);
                // max name length here based on constants in SqlSemaphore.cs
                (safeName.Length <= (115 - 19)).ShouldEqual(true, "was: " + safeName);
                Regex.IsMatch(safeName, @"^[a-zA-Z0-9]+$").ShouldEqual(true, "was: " + safeName);
                return safeName;
            }

            ToSafeNameChecked(string.Empty);
            ToSafeNameChecked("a b");
            ToSafeNameChecked(new string('a', 1000));
            ToSafeNameChecked(string.Join(string.Empty, Enumerable.Range(0, byte.MaxValue).Select(i => (char)i)));

            Assert.AreNotEqual(ToSafeNameChecked(new string('b', 500)), ToSafeNameChecked(new string('b', 499) + "B"));

            ToSafeNameChecked(new string('x', 200)).Length.ShouldEqual(115 - 30);

            Enumerable.Range(0, 1000)
                .Select(i => ToSafeNameChecked(i.ToString()))
                .Distinct()
                .Count()
                .ShouldEqual(1000);
        }

        [TestMethod]
        public void TestNameManglingCompatibility()
        {
            SqlSemaphore.ToSafeName(string.Empty).ShouldEqual("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855semaphore");
            SqlSemaphore.ToSafeName("a_simple_name").ShouldEqual("a5fsimple5fn5becacaa1afce7173bf71d20caf31364c2b10c21f7490c942fdc45467aba2d2asemaphore");
            SqlSemaphore.ToSafeName("a").ShouldEqual("aca978112ca1bbdcafac231b39a23dc4da786eff8147c4e72b9807785afee48bbsemaphore");
            SqlSemaphore.ToSafeName("A").ShouldEqual("A559aead08264d5795d3909718cdd05abd49572e84fe55590eef31a88a08fdffdsemaphore");
            SqlSemaphore.ToSafeName("0").ShouldEqual("05feceb66ffc86f38d952786c6d696c79c2dbc239dd4e91b46729d73a27fb57e9semaphore");
            SqlSemaphore.ToSafeName("!?#").ShouldEqual("213f231be5b6313c68d3c674c3b17246eaaa3222fe5bc23d9173ac6f58319c6004d6bfsemaphore");
            SqlSemaphore.ToSafeName(string.Join(string.Empty, Enumerable.Range(0, byte.MaxValue).Select(i => (char)i)))
                .ShouldEqual("0123456789ab7fb98786c16c175d232ab161b5e604c5792e6befd4e1e8d4ecac9d568a6db524semaphore");
        }

        [TestMethod]
        public void TestTicketsTakenOnBothConnectionAndTransactionForThatConnection()
        {
            using (var connection = new SqlConnection(ConnectionStringProvider.ConnectionString))
            {
                connection.Open();

                var semaphore1 = new SqlDistributedSemaphore(nameof(TestTicketsTakenOnBothConnectionAndTransactionForThatConnection), 2, connection);
                var handle1 = semaphore1.Acquire();

                using (var transaction = connection.BeginTransaction())
                {
                    var semaphore2 = new SqlDistributedSemaphore(nameof(TestTicketsTakenOnBothConnectionAndTransactionForThatConnection), 2, transaction);
                    var handle2 = semaphore2.Acquire();
                    semaphore2.TryAcquire().ShouldEqual(null);
                    var ex = TestHelper.AssertThrows<InvalidOperationException>(() => semaphore2.Acquire());
                    ex.Message.Contains("Deadlock").ShouldEqual(true, ex.ToString());
                }
            }
        }
    }
}
