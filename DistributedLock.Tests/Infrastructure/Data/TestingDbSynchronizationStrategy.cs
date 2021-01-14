using Medallion.Threading.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;

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

        public string SetUniqueApplicationName(string baseName = "")
        {
            // note: due to retries, we incorporate a GUID here to ensure that we have a fresh connection pool
            var applicationName = DistributedLockHelpers.ToSafeName(
                $"{(baseName.Length > 0 ? baseName + "_" : string.Empty)}{TestContext.CurrentContext.Test.FullName}_{TargetFramework.Current}_{Guid.NewGuid()}",
                maxNameLength: this.Db.MaxApplicationNameLength, 
                s => s
            );
            this.Db.ConnectionStringBuilder["Application Name"] = applicationName;
            return applicationName;
        }
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

        public sealed override TestingDbConnectionOptions GetConnectionOptions() =>
            new TestingDbConnectionOptions 
            { 
                ConnectionString = this.Db.ConnectionStringBuilder.ConnectionString, 
                ConnectionStringUseMultiplexing = this.UseMultiplexingNotTransaction == true,
                ConnectionStringUseTransaction = this.UseMultiplexingNotTransaction == false,
                ConnectionStringKeepaliveCadence = this.KeepaliveCadence,
            };

        public sealed override IDisposable? PrepareForHandleLost() =>
            new HandleLostScope(this.SetUniqueApplicationName(nameof(PrepareForHandleLost)), this.Db);

        private class HandleLostScope : IDisposable
        {
            private string? _applicationName;
            private readonly TDb _db;

            public HandleLostScope(string applicationName, TDb testingDb)
            {
                this._applicationName = applicationName;
                this._db = testingDb;
            }

            public void Dispose()
            {
                var applicationName = Interlocked.Exchange(ref this._applicationName, null);
                if (applicationName != null)
                {
                    this._db.KillSessionsAsync(applicationName).Wait();
                }
            }
        }
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

        protected abstract void EndAmbient();

        /// <summary>
        /// If <see cref="StartAmbient"/> has been called, returns the current ambient connection
        /// </summary>
        public abstract DbConnection? AmbientConnection { get; }

        public sealed override IDisposable? PrepareForHandleLost()
        {
            this.StartAmbient();
            return this.AmbientConnection;
        }

        public sealed override void PrepareForHandleAbandonment() => this.StartAmbient();

        public sealed override void PerformAdditionalCleanupForHandleAbandonment()
        {
            this.AmbientConnection!.Dispose();
            using var connection = this.Db.CreateConnection();
            this.Db.ClearPool(connection);
            this.EndAmbient();
        }
    }

    public sealed class TestingExternalConnectionSynchronizationStrategy<TDb> : TestingExternalConnectionOrTransactionSynchronizationStrategy<TDb>
        where TDb : ITestingDb, new()
    {
        private readonly DisposableCollection _disposables = new DisposableCollection();
        private DbConnection? _ambientConnection;

        public override DbConnection? AmbientConnection => this._ambientConnection;

        public override void StartAmbient()
        {
            // clear first so GetConnectionOptions will make a new connection
            this._ambientConnection = null;

            this._ambientConnection = this.GetConnectionOptions().Connection;
        }

        protected override void EndAmbient() => this._ambientConnection = null;

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
        public override DbConnection? AmbientConnection => this.AmbientTransaction?.Connection;

        public override void StartAmbient()
        {
            // clear first so GetConnectionOptions will make a new transaction
            this.AmbientTransaction = null;

            this.AmbientTransaction = this.GetConnectionOptions().Transaction;
        }

        protected override void EndAmbient() => this.AmbientTransaction = null;

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

        public override void Dispose()
        {
            this._disposables.Dispose();
            base.Dispose();
        }
    }
}
