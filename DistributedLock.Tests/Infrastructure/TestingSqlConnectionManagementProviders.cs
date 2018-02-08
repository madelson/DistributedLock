using Medallion.Threading.Sql;
using Medallion.Threading.Sql.ConnectionMultiplexing;
using System;
using System.Collections.Generic;
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

    public sealed class ConnectionProvider : TestingSqlConnectionManagementProvider, IExternalConnectionOrTransactionTestingSqlConnectionManagementProvider
    {
        private static volatile ConnectionHolder current;

        public override ConnectionInfo GetConnectionInfo()
        {
            var currentConnection = current?.Connection;
            if (currentConnection != null)
            {
                return new ConnectionInfo { Connection = currentConnection };
            }
            
            var connection = new SqlConnection(ConnectionStringProvider.ConnectionString);
            this.RegisterCleanupAction(CreateWeakDisposeAction(connection));
            connection.Open();
            return new ConnectionInfo { Connection = connection };
        }

        public static IDisposable UseConnection(SqlConnection connection)
        {
            if (current?.Connection != null) { throw new InvalidOperationException("already set"); }

            return current = new ConnectionHolder(connection);
        }

        /// <summary>
        /// sp_getapplock is reentrant on the same connection
        /// </summary>
        internal override bool IsReentrantForAppLock => true;

        private sealed class ConnectionHolder : IDisposable
        {
            private volatile SqlConnection connection;

            public ConnectionHolder(SqlConnection connection)
            {
                this.connection = connection;
            }

            public SqlConnection Connection => this.connection;
            
            void IDisposable.Dispose()
            {
                this.connection = null;
            }
        }
    }

    public sealed class TransactionProvider : TestingSqlConnectionManagementProvider, IExternalConnectionOrTransactionTestingSqlConnectionManagementProvider
    {
        private static volatile TransactionHolder current;

        public override ConnectionInfo GetConnectionInfo()
        {
            var currentTransaction = current?.Transaction;
            if (currentTransaction != null)
            {
                return new ConnectionInfo { Transaction = currentTransaction };
            }

            var connection = new SqlConnection(ConnectionStringProvider.ConnectionString);
            this.RegisterCleanupAction(CreateWeakDisposeAction(connection));
            connection.Open();
            var transaction = connection.BeginTransaction();
            this.RegisterCleanupAction(CreateWeakDisposeAction(transaction));
            return new ConnectionInfo { Transaction = transaction };
        }

        public static IDisposable UseTransaction(SqlTransaction transaction)
        {
            if (current?.Transaction != null) { throw new InvalidOperationException("already set"); }

            return current = new TransactionHolder(transaction);
        }

        /// <summary>
        /// sp_getapplock is reentrant on the same connection
        /// </summary>
        internal override bool IsReentrantForAppLock => true;

        private sealed class TransactionHolder : IDisposable
        {
            private volatile SqlTransaction transaction;

            public TransactionHolder(SqlTransaction transaction)
            {
                this.transaction = transaction;
            }

            public SqlTransaction Transaction => this.transaction;

            void IDisposable.Dispose()
            {
                this.transaction = null;
            }
        }
    }
}
