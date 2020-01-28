using DistributedLock.Tests;
using Medallion.Threading.Sql;
using Medallion.Threading.Sql.ConnectionMultiplexing;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    public abstract class ConnectionStringProvider : TestingSqlConnectionManagementProvider
    {
        internal static readonly string ConnectionString = new SqlConnectionStringBuilder
            {
                DataSource = @".\SQLEXPRESS",
                InitialCatalog = "master",
                IntegratedSecurity = true,
                ApplicationName = typeof(ConnectionStringProvider).Assembly.GetName().Name,
                // set a high pool size so that we don't empty the pool through things like lock abandonment tests
                MaxPoolSize = 10000,
            }
            .ConnectionString;

        private static volatile string currentConnectionString = ConnectionString;

        private readonly SqlDistributedLockConnectionStrategy? _strategy;

        public ConnectionStringProvider(SqlDistributedLockConnectionStrategy? strategy)
        {
            this._strategy = strategy;
        }

        public override ConnectionInfo GetConnectionInfo() => new ConnectionInfo { ConnectionString = currentConnectionString, Strategy = this._strategy };

        /// <summary>
        /// Since every lock handle has a dedicated connection (even in the case of multiplexing we don't share a connection for two acquires
        /// on the same name, we are not reentrant)
        /// </summary>
        internal override bool IsReentrantForAppLock => false;

        public static IDisposable UseConnectionString(string connectionString)
        {
            currentConnectionString = connectionString;
            return new ReleaseAction(() => currentConnectionString = ConnectionString);
        }
    }

    public sealed class NoStrategyConnectionStringProvider : ConnectionStringProvider
    {
        public NoStrategyConnectionStringProvider() : base(null) { }
    }

    public sealed class DefaultConnectionStringProvider : ConnectionStringProvider
    {
        public DefaultConnectionStringProvider() : base(SqlDistributedLockConnectionStrategy.Default) { }
    }

    public sealed class AzureConnectionStringProvider : ConnectionStringProvider
    {
        public AzureConnectionStringProvider() : base(SqlDistributedLockConnectionStrategy.Azure) { }
    }

    public sealed class ConnectionBasedConnectionStringProvider : ConnectionStringProvider
    {
        public ConnectionBasedConnectionStringProvider() : base(SqlDistributedLockConnectionStrategy.Connection) { }
    }

    public sealed class TransactionBasedConnectionStringProvider : ConnectionStringProvider
    {
        public TransactionBasedConnectionStringProvider() : base(SqlDistributedLockConnectionStrategy.Transaction) { }
    }

    public sealed class MultiplexedConnectionStringProvider : ConnectionStringProvider
    {
        public MultiplexedConnectionStringProvider() : base(SqlDistributedLockConnectionStrategy.OptimisticConnectionMultiplexing) { }

        internal override void PerformCleanupForLockAbandonment()
        {
            // normally this happens on a cadence, but here we force it one-off
            MultiplexedConnectionLockPool.Instance.ThreadSafeDoCleanupAsync().Wait();

            // still do this because upgrade locks don't allow multiplexing
            base.PerformCleanupForLockAbandonment();
        }
    }

    public abstract class ConnectionProvider : TestingSqlConnectionManagementProvider, IExternalConnectionOrTransactionTestingSqlConnectionManagementProvider
    {
        private static readonly AsyncLocal<ConnectionHolder?> Current = new AsyncLocal<ConnectionHolder?>();

        public sealed override ConnectionInfo GetConnectionInfo()
        {
            var currentConnection = Current.Value?.Connection;
            if (currentConnection != null)
            {
                return new ConnectionInfo { Connection = currentConnection };
            }
            
            var connection = this.CreateConnection(ConnectionStringProvider.ConnectionString);
            this.RegisterCleanupAction(CreateWeakDisposeAction(connection));
            connection.Open();
            return new ConnectionInfo { Connection = connection };
        }

        public static IDisposable UseConnection(DbConnection connection)
        {
            if (Current.Value?.Connection != null) { throw new InvalidOperationException("already set"); }

            return Current.Value = new ConnectionHolder(connection);
        }

        /// <summary>
        /// sp_getapplock is reentrant on the same connection
        /// </summary>
        internal sealed override bool IsReentrantForAppLock => true;

        protected abstract DbConnection CreateConnection(string connectionString);

        private sealed class ConnectionHolder : IDisposable
        {
            private volatile DbConnection? connection;

            public ConnectionHolder(DbConnection connection)
            {
                this.connection = connection;
            }

            public DbConnection? Connection => this.connection;
            
            void IDisposable.Dispose()
            {
                this.connection = null;
            }
        }
    }

    public sealed class DefaultClientConnectionProvider : ConnectionProvider
    {
        protected override DbConnection CreateConnection(string connectionString) =>
            SqlClientHelper.CreateConnection(connectionString);
    }

    public sealed class AlternateClientConnectionProvider : ConnectionProvider
    {
        protected override DbConnection CreateConnection(string connectionString) =>
            SqlTestHelper.CreateAlternateProviderConnection(connectionString);
    }

    public abstract class TransactionProvider : TestingSqlConnectionManagementProvider, IExternalConnectionOrTransactionTestingSqlConnectionManagementProvider
    {
        private static readonly AsyncLocal<TransactionHolder?> Current = new AsyncLocal<TransactionHolder?>();

        public sealed override ConnectionInfo GetConnectionInfo()
        {
            var currentTransaction = Current.Value?.Transaction;
            if (currentTransaction != null)
            {
                return new ConnectionInfo { Transaction = currentTransaction };
            }

            var connection = SqlClientHelper.CreateConnection(ConnectionStringProvider.ConnectionString);
            this.RegisterCleanupAction(CreateWeakDisposeAction(connection));
            connection.Open();
            var transaction = connection.BeginTransaction();
            this.RegisterCleanupAction(CreateWeakDisposeAction(transaction));
            return new ConnectionInfo { Transaction = transaction };
        }

        public static IDisposable UseTransaction(DbTransaction transaction)
        {
            if (Current.Value?.Transaction != null) { throw new InvalidOperationException("already set"); }

            return Current.Value = new TransactionHolder(transaction);
        }

        /// <summary>
        /// sp_getapplock is reentrant on the same connection
        /// </summary>
        internal sealed override bool IsReentrantForAppLock => true;

        protected abstract DbConnection CreateConnection(string connectionString);

        private sealed class TransactionHolder : IDisposable
        {
            private volatile DbTransaction? transaction;

            public TransactionHolder(DbTransaction transaction)
            {
                this.transaction = transaction;
            }

            public DbTransaction? Transaction => this.transaction;

            void IDisposable.Dispose()
            {
                this.transaction = null;
            }
        }
    }

    public sealed class DefaultClientTransactionProvider : TransactionProvider
    {
        protected override DbConnection CreateConnection(string connectionString) =>
            SqlClientHelper.CreateConnection(connectionString);
    }

    public sealed class AlternateClientTransactionProvider : TransactionProvider
    {
        protected override DbConnection CreateConnection(string connectionString) =>
            SqlTestHelper.CreateAlternateProviderConnection(connectionString);
    }
}
