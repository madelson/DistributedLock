using Medallion.Threading.Data;
using Medallion.Threading.Internal;
using Medallion.Threading.SqlServer;
using Medallion.Threading.Tests.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Medallion.Threading.Tests.SqlServer
{
    public sealed class SqlServerProvider : ITestingDbProvider, ITestingDbConnectionProvider<SqlServerProvider>, 
        ITestingDbLockProvider<SqlServerProvider>, ITestingDbReaderWriterLockProvider<SqlServerProvider>, 
        ITestingDbUpgradeableReaderWriterLockProvider<SqlServerProvider>, ITestingDbSemaphoreProvider<SqlServerProvider>
    {
        internal static readonly string ConnectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
            {
                DataSource = @".\SQLEXPRESS",
                InitialCatalog = "master",
                IntegratedSecurity = true,
                ApplicationName = $"{typeof(SqlServerProvider).Assembly.GetName().Name} ({TestHelper.FrameworkName})",
                // set a high pool size so that we don't empty the pool through things like lock abandonment tests
                MaxPoolSize = 10000,
            }
            .ConnectionString;

        string ITestingDbProvider.ConnectionString => ConnectionString;

        IDbConnection ITestingDbConnectionProvider<SqlServerProvider>.CreateConnection() => new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
        void ITestingDbConnectionProvider<SqlServerProvider>.ClearPool(IDbConnection connection) =>
            Microsoft.Data.SqlClient.SqlConnection.ClearPool((Microsoft.Data.SqlClient.SqlConnection)connection);

        IDistributedLock ITestingDbLockProvider<SqlServerProvider>.CreateLockWithExactName(string name, ConnectionStrategy<SqlServerProvider> connectionStrategy) =>
            connectionStrategy.Create(
                (connectionString, useTransaction) =>
                    new SqlDistributedLock(name, connectionString, useTransaction ? SqlDistributedLockConnectionStrategy.Transaction : SqlDistributedLockConnectionStrategy.OptimisticConnectionMultiplexing, exactName: true),
                connection => new SqlDistributedLock(name, connection, exactName: true),
                transaction => new SqlDistributedLock(name, transaction, exactName: true)
            );

        IDistributedUpgradeableReaderWriterLock ITestingDbUpgradeableReaderWriterLockProvider<SqlServerProvider>.CreateLockWithExactName(string name, ConnectionStrategy<SqlServerProvider> connectionStrategy) =>
            connectionStrategy.Create(
                (connectionString, useTransaction) =>
                    new SqlDistributedReaderWriterLock(name, connectionString, useTransaction ? SqlDistributedLockConnectionStrategy.Transaction : SqlDistributedLockConnectionStrategy.OptimisticConnectionMultiplexing, exactName: true),
                connection => new SqlDistributedReaderWriterLock(name, connection, exactName: true),
                transaction => new SqlDistributedReaderWriterLock(name, transaction, exactName: true)
            );

        IDistributedReaderWriterLock ITestingDbReaderWriterLockProvider<SqlServerProvider>.CreateLockWithExactName(string name, ConnectionStrategy<SqlServerProvider> connectionStrategy) =>
            this.As<ITestingDbUpgradeableReaderWriterLockProvider<SqlServerProvider>>().CreateLockWithExactName(name, connectionStrategy);

        SqlDistributedSemaphore ITestingDbSemaphoreProvider<SqlServerProvider>.CreateSemaphoreWithExactName(string name, int maxCount, ConnectionStrategy<SqlServerProvider> connectionStrategy) =>
            connectionStrategy.Create(
                (connectionString, useTransaction) =>
                    new SqlDistributedSemaphore(name, maxCount, connectionString, useTransaction ? SqlDistributedLockConnectionStrategy.Transaction : SqlDistributedLockConnectionStrategy.OptimisticConnectionMultiplexing),
                connection => new SqlDistributedSemaphore(name, maxCount, connection),
                transaction => new SqlDistributedSemaphore(name, maxCount, transaction)
            );

        string ITestingDbLockProvider<SqlServerProvider>.GetSafeName(string name) => SqlDistributedLock.GetSafeName(name);
        string ITestingDbReaderWriterLockProvider<SqlServerProvider>.GetSafeName(string name) => SqlDistributedReaderWriterLock.GetSafeName(name);
        string ITestingDbUpgradeableReaderWriterLockProvider<SqlServerProvider>.GetSafeName(string name) => SqlDistributedReaderWriterLock.GetSafeName(name);
        string ITestingDbSemaphoreProvider<SqlServerProvider>.GetSafeName(string name) => name ?? throw new ArgumentNullException(nameof(name));
    }

    public sealed class SystemDataSqlServerProvider : ITestingDbConnectionProvider<SqlServerProvider>
    {
        public IDbConnection CreateConnection() => new System.Data.SqlClient.SqlConnection(SqlServerProvider.ConnectionString);

        public void ClearPool(IDbConnection connection) => System.Data.SqlClient.SqlConnection.ClearPool((System.Data.SqlClient.SqlConnection)connection);
    }
}
