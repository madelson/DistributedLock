using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Oracle
{
    public class OracleDistributedLockTest
    {
        [Test]
        public async Task Test()
        {
            var connection = new OracleConnection(OracleCredentials.GetConnectionString(TestContext.CurrentContext.TestDirectory));
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT 10 FROM DUAL";
            (await command.ExecuteScalarAsync()).ShouldEqual(10);
        }
    }
}
