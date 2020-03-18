using Medallion.Threading.Postgres;
using Medallion.Threading.Tests.Data;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Medallion.Threading.Tests.Postgres
{
    public sealed class PostgresProvider : ITestingDbProvider, ITestingDbConnectionProvider<PostgresProvider>, ITestingDbLockProvider<PostgresProvider>
    {
        public static readonly string ConnectionString = PostgresCredentials.GetConnectionString(TestContext.CurrentContext.TestDirectory);

        string ITestingDbProvider.ConnectionString => ConnectionString;

        IDbConnection ITestingDbConnectionProvider<PostgresProvider>.CreateConnection() => new NpgsqlConnection(ConnectionString);
        void ITestingDbConnectionProvider<PostgresProvider>.ClearPool(IDbConnection connection) =>
            NpgsqlConnection.ClearPool((NpgsqlConnection)connection);

        IDistributedLock ITestingDbLockProvider<PostgresProvider>.CreateLockWithExactName(string name, ConnectionStrategy<PostgresProvider> connectionStrategy)
        {
            var key = new PostgresAdvisoryLockKey(name, allowHashing: false);
            return connectionStrategy.Create(
                (connectionString, useTransaction) => new PostgresDistributedLock(key, connectionString),
                connection => new PostgresDistributedLock(key, connection),
                transaction => new PostgresDistributedLock(key, transaction.Connection)
            );
        }

        public string GetSafeName(string name) => PostgresDistributedLock.GetSafeName(name).ToString();
    }
}
