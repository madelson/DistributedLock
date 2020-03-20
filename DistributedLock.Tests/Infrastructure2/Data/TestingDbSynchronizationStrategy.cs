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
    }

    public sealed class TestingConnectionMultiplexingSynchronizationStrategy<TDb> : TestingDbSynchronizationStrategy<TDb>
        where TDb : ITestingDb, new()
    {
        public override TestingDbConnectionOptions GetConnectionOptions() =>
            new TestingDbConnectionOptions { ConnectionString = this.Db.ConnectionStringBuilder.ConnectionString, ConnectionStringOptions = TestingConnectionStringOptions.UseMultiplexing };
    }

    public abstract class TestingOwnedConnectionSynchronizationStrategy<TDb> : TestingDbSynchronizationStrategy<TDb>
        where TDb : ITestingDb, new()
    {
    }

    public sealed class TestingConnectionStringSynchronizationStrategy<TDb> : TestingOwnedConnectionSynchronizationStrategy<TDb>
        where TDb : ITestingDb, new()
    {
        public override TestingDbConnectionOptions GetConnectionOptions() =>
            new TestingDbConnectionOptions { ConnectionString = this.Db.ConnectionStringBuilder.ConnectionString };
    }

    public sealed class TestingConnectionStringWithTransactionSynchronizationStrategy<TDb> : TestingOwnedConnectionSynchronizationStrategy<TDb>
        where TDb : ITestingDb, new()
    {
        public override TestingDbConnectionOptions GetConnectionOptions() =>
            new TestingDbConnectionOptions { ConnectionString = this.Db.ConnectionStringBuilder.ConnectionString, ConnectionStringOptions = TestingConnectionStringOptions.UseTransaction };
    }

    public abstract class TestingExternalConnectionOrTransactionSynchronizationStrategy<TDb> : TestingDbSynchronizationStrategy<TDb>
        where TDb : ITestingDb, new()
    {
        public abstract void StartAmbient();
    }

    public sealed class TestingExternalConnectionSynchronizationStrategy<TDb> : TestingExternalConnectionOrTransactionSynchronizationStrategy<TDb>
        where TDb : ITestingDb, new()
    {
        private readonly DisposableCollection _disposables = new DisposableCollection();
        private DbConnection? _ambientConnection;

        public override void StartAmbient()
        {
            // clear first so GetConnectionOptions will make a new connection
            this._ambientConnection = null;

            this._ambientConnection = this.GetConnectionOptions().Connection;
        }

        public override TestingDbConnectionOptions GetConnectionOptions()
        {
            DbConnection connection;
            if (this._ambientConnection != null)
            {
                connection = this._ambientConnection;
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
            if (this._ambientConnection != null) { throw new InvalidOperationException("cannot perform abandonment cleanup with an ambient connection"); }
            this._disposables.ClearAndDisposeAll();
            using var connection = this.Db.CreateConnection();
            this.Db.ClearPool(connection);
        }

        public override void Dispose() => this._disposables.Dispose();
    }

    public sealed class TestingExternalTransactionSynchronizationStrategy<TDb> : TestingExternalConnectionOrTransactionSynchronizationStrategy<TDb>
        where TDb : ITestingDb, new()
    {
        private readonly DisposableCollection _disposables = new DisposableCollection();
        private DbTransaction? _ambientTransaction;

        public override void StartAmbient()
        {
            // clear first so GetConnectionOptions will make a new transaction
            this._ambientTransaction = null;

            this._ambientTransaction = this.GetConnectionOptions().Transaction;
        }

        public override TestingDbConnectionOptions GetConnectionOptions()
        {
            DbTransaction transaction;
            if (this._ambientTransaction != null)
            {
                transaction = this._ambientTransaction;
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
            if (this._ambientTransaction != null) { throw new InvalidOperationException("cannot perform abandonment cleanup with an ambient transaction"); }
            this._disposables.ClearAndDisposeAll();
            using var connection = this.Db.CreateConnection();
            this.Db.ClearPool(connection);
        }

        public override void Dispose() => this._disposables.Dispose();
    }
}
