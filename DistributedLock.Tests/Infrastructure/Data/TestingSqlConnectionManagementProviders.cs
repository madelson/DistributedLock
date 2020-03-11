using Medallion.Threading.Data;
using Medallion.Threading.Internal;
using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace Medallion.Threading.Tests.Data
{
    public abstract class ConnectionStringProvider : TestingSqlConnectionManagementProvider
    {
        internal static readonly string ConnectionString = new SqlConnectionStringBuilder
            {
                DataSource = @".\SQLEXPRESS",
                InitialCatalog = "master",
                IntegratedSecurity = true,
                ApplicationName = $"{typeof(ConnectionStringProvider).Assembly.GetName().Name} ({TestHelper.FrameworkName})",
                // set a high pool size so that we don't empty the pool through things like lock abandonment tests
                MaxPoolSize = 10000,
            }
            .ConnectionString;

        private readonly SqlDistributedLockConnectionStrategy? _strategy;

        public ConnectionStringProvider(SqlDistributedLockConnectionStrategy? strategy)
        {
            this._strategy = strategy;
        }

        public override ConnectionInfo GetConnectionInfo() => new ConnectionInfo 
        { 
            ConnectionString = Current<ConnectionStringBox>.Value?.ConnectionString ?? ConnectionString, 
            Strategy = this._strategy
        };

        /// <summary>
        /// Since every lock handle has a dedicated connection (even in the case of multiplexing we don't share a connection for two acquires
        /// on the same name, we are not reentrant)
        /// </summary>
        internal override bool IsReentrantForAppLock => false;

        public static IDisposable UseConnectionString(string connectionString) => 
            Current<ConnectionStringBox>.Use(new ConnectionStringBox(connectionString));

        // to avoid claimin Current<string>
        private sealed class ConnectionStringBox
        {
            public ConnectionStringBox(string connectionString)
            {
                this.ConnectionString = connectionString;
            }

            public string ConnectionString { get; }
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
            ManagedFinalizerQueue.Instance.FinalizeAsync().Wait();

            // still do this because upgrade locks don't allow multiplexing
            base.PerformCleanupForLockAbandonment();
        }
    }

    public abstract class ConnectionProvider : TestingSqlConnectionManagementProvider, IExternalConnectionOrTransactionTestingSqlConnectionManagementProvider
    {
        public sealed override ConnectionInfo GetConnectionInfo()
        {
            var currentConnection = Current<DbConnection>.Value;
            if (currentConnection != null)
            {
                return new ConnectionInfo { Connection = currentConnection };
            }
            
            var connection = this.CreateConnection(ConnectionStringProvider.ConnectionString);
            this.RegisterCleanupAction(CreateWeakDisposeAction(connection));
            connection.Open();
            return new ConnectionInfo { Connection = connection };
        }

        public static IDisposable UseConnection(DbConnection connection) => Current<DbConnection>.Use(connection);

        /// <summary>
        /// sp_getapplock is reentrant on the same connection
        /// </summary>
        internal sealed override bool IsReentrantForAppLock => true;

        protected abstract DbConnection CreateConnection(string connectionString);
    }

    public sealed class DefaultClientConnectionProvider : ConnectionProvider
    {
        protected override DbConnection CreateConnection(string connectionString) =>
            new SqlConnection(connectionString);
    }

    public sealed class AlternateClientConnectionProvider : ConnectionProvider
    {
        protected override DbConnection CreateConnection(string connectionString) =>
            SqlTestHelper.CreateAlternateProviderConnection(connectionString);
    }

    public abstract class TransactionProvider : TestingSqlConnectionManagementProvider, IExternalConnectionOrTransactionTestingSqlConnectionManagementProvider
    {
        public sealed override ConnectionInfo GetConnectionInfo()
        {
            var currentTransaction = Current<DbTransaction>.Value;
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

        public static IDisposable UseTransaction(DbTransaction transaction) => Current<DbTransaction>.Use(transaction);

        /// <summary>
        /// sp_getapplock is reentrant on the same connection
        /// </summary>
        internal sealed override bool IsReentrantForAppLock => true;

        protected abstract DbConnection CreateConnection(string connectionString);
    }

    public sealed class DefaultClientTransactionProvider : TransactionProvider
    {
        protected override DbConnection CreateConnection(string connectionString) =>
            new SqlConnection(connectionString);
    }

    public sealed class AlternateClientTransactionProvider : TransactionProvider
    {
        protected override DbConnection CreateConnection(string connectionString) =>
            SqlTestHelper.CreateAlternateProviderConnection(connectionString);
    }
}
