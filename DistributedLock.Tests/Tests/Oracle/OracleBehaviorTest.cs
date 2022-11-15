using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Oracle;

public class OracleBehaviorTest
{
    [Test]
    public async Task BasicConnectivityTest()
    {
        using var connection = new OracleConnection(OracleCredentials.GetConnectionString(TestContext.CurrentContext.TestDirectory));
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 10 FROM DUAL";
        (await command.ExecuteScalarAsync()).ShouldEqual(10);
    }

    [Test]
    public async Task TestCommandImplicitlyParticipatesInTransaction()
    {
        using var connection = new OracleConnection(OracleCredentials.GetConnectionString(TestContext.CurrentContext.TestDirectory));
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction.ShouldEqual(transaction);
    }
}
