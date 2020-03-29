using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Medallion.Threading.Tests.Data
{
    /// <summary>
    /// Determines how an ADO.NET-based synchronization primitive should function
    /// </summary>
    public abstract class TestingDbSynchronizationStrategy : TestingSynchronizationStrategy
    {
        protected TestingDbSynchronizationStrategy(ITestingDb db)
        {
            this.Db = db;
        }

        public ITestingDb Db { get; }

        public abstract TestingDbConnectionOptions GetConnectionOptions();
    }

    public abstract class TestingDbSynchronizationStrategy<TDb> : TestingDbSynchronizationStrategy
        where TDb : ITestingDb, new()
    {
        protected TestingDbSynchronizationStrategy() : base(new TDb()) { }

        public new TDb Db => (TDb)base.Db;

        public override void Dispose()
        {
            // if we have a uniquely-named connection, clear it's pool to avoid "leaking" connections into pools we'll never
            // use again
            if (!Equals(this.Db.ConnectionStringBuilder["Application Name"], new TDb().ConnectionStringBuilder["Application Name"]))
            {
                using var connection = this.Db.CreateConnection();
                this.Db.ClearPool(connection);
            }

            base.Dispose();
        }
    }

    public abstract class TestingConnectionStringSynchronizationStrategy<TDb> : TestingDbSynchronizationStrategy<TDb>
        // since we're just going to be generating from connection strings, we only care about
        // the primary ADO client for the database
        where TDb : ITestingPrimaryClientDb, new()
    {
        protected abstract bool? UseMultiplexingNotTransaction { get; }
        public TimeSpan? KeepaliveCadence { get; set; }

        public override TestingDbConnectionOptions GetConnectionOptions() =>
            new TestingDbConnectionOptions 
            { 
                ConnectionString = this.Db.ConnectionStringBuilder.ConnectionString, 
                ConnectionStringUseMultiplexing = this.UseMultiplexingNotTransaction == true,
                ConnectionStringUseTransaction = this.UseMultiplexingNotTransaction == false,
                ConnectionStringKeepaliveCadence = this.KeepaliveCadence,
            };
    }

    public sealed class TestingConnectionMultiplexingSynchronizationStrategy<TDb> : TestingConnectionStringSynchronizationStrategy<TDb>
        where TDb : ITestingPrimaryClientDb, new()
    {
        protected override bool? UseMultiplexingNotTransaction => true;
    }

    public sealed class TestingOwnedConnectionSynchronizationStrategy<TDb> : TestingConnectionStringSynchronizationStrategy<TDb>
        where TDb : ITestingPrimaryClientDb, new()
    {
        protected override bool? UseMultiplexingNotTransaction => null;
    }

    public sealed class TestingOwnedTransactionSynchronizationStrategy<TDb> : TestingConnectionStringSynchronizationStrategy<TDb>
        where TDb : ITestingPrimaryClientDb, new()
    {
        protected override bool? UseMultiplexingNotTransaction => false;
    }

    public abstract class TestingExternalConnectionOrTransactionSynchronizationStrategy<TDb> : TestingDbSynchronizationStrategy<TDb>
        where TDb : ITestingDb, new()
    {
        /// <summary>
        /// Starts a new "ambient" connection or transaction that future locks will be created with
        /// </summary>
        public abstract void StartAmbient();
    }

    public sealed class TestingExternalConnectionSynchronizationStrategy<TDb> : TestingExternalConnectionOrTransactionSynchronizationStrategy<TDb>
        where TDb : ITestingDb, new()
    {
        private readonly DisposableCollection _disposables = new DisposableCollection();

        public DbConnection? AmbientConnection { get; private set; }

        public override void StartAmbient()
        {
            // clear first so GetConnectionOptions will make a new connection
            this.AmbientConnection = null;

            this.AmbientConnection = this.GetConnectionOptions().Connection;
        }

        public override TestingDbConnectionOptions GetConnectionOptions()
        {
            DbConnection connection;
            if (this.AmbientConnection != null)
            {
                connection = this.AmbientConnection;
            }
            else
            {
                connection = this.Db.CreateConnection();
                this._disposables.Add(connection);
                connection.Open();
            }
            return new TestingDbConnectionOptions { Connection = connection };
        }

        public override void PerformAdditionalCleanupForHandleAbandonment()
        {
            if (this.AmbientConnection != null) { throw new InvalidOperationException("cannot perform abandonment cleanup with an ambient connection"); }
            this._disposables.ClearAndDisposeAll();
            using var connection = this.Db.CreateConnection();
            this.Db.ClearPool(connection);
        }

        public override void Dispose()
        {
            this._disposables.Dispose();
            base.Dispose();
        }
    }

    public sealed class TestingExternalTransactionSynchronizationStrategy<TDb> : TestingExternalConnectionOrTransactionSynchronizationStrategy<TDb>
        where TDb : ITestingDb, new()
    {
        private readonly DisposableCollection _disposables = new DisposableCollection();

        public DbTransaction? AmbientTransaction { get; private set; }

        public override void StartAmbient()
        {
            // clear first so GetConnectionOptions will make a new transaction
            this.AmbientTransaction = null;

            this.AmbientTransaction = this.GetConnectionOptions().Transaction;
        }

        public override TestingDbConnectionOptions GetConnectionOptions()
        {
            DbTransaction transaction;
            if (this.AmbientTransaction != null)
            {
                transaction = this.AmbientTransaction;
            }
            else
            {
                var connection = this.Db.CreateConnection();
                this._disposables.Add(connection);
                connection.Open();
                transaction = connection.BeginTransaction();
                this._disposables.Add(transaction);
            }

            return new TestingDbConnectionOptions { Transaction = transaction };
        }

        public override void PerformAdditionalCleanupForHandleAbandonment()
        {
            if (this.AmbientTransaction != null) { throw new InvalidOperationException("cannot perform abandonment cleanup with an ambient transaction"); }
            this._disposables.ClearAndDisposeAll();
            using var connection = this.Db.CreateConnection();
            this.Db.ClearPool(connection);
        }

        public override void Dispose()
        {
            this._disposables.Dispose();
            base.Dispose();
        }
    }
}
