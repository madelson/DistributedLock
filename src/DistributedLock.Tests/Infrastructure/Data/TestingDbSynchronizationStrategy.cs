using System.Data.Common;

namespace Medallion.Threading.Tests.Data;

/// <summary>
/// Determines how an ADO.NET-based synchronization primitive should function
/// </summary>
public abstract class TestingDbSynchronizationStrategy : TestingSynchronizationStrategy
{
    protected TestingDbSynchronizationStrategy(TestingDb db)
    {
        this.Db = db;
    }

    public TestingDb Db { get; }

    public abstract TestingDbConnectionOptions GetConnectionOptions();

    public override void PrepareForHighContention(ref int maxConcurrentAcquires) =>
        this.Db.PrepareForHighContention(ref maxConcurrentAcquires);

    public override ValueTask SetupAsync() => this.Db.SetupAsync();
    public override ValueTask DisposeAsync() => this.Db.DisposeAsync();
}

public abstract class TestingDbSynchronizationStrategy<TDb> : TestingDbSynchronizationStrategy
    where TDb : TestingDb, new()
{
    protected TestingDbSynchronizationStrategy() : base(new TDb()) { }

    public new TDb Db => (TDb)base.Db;
}

public abstract class TestingConnectionStringSynchronizationStrategy<TDb> : TestingDbSynchronizationStrategy<TDb>
    // since we're just going to be generating from connection strings, we only care about
    // the primary ADO client for the database
    where TDb : TestingPrimaryClientDb, new()
{
    protected abstract bool? UseMultiplexingNotTransaction { get; }
    public TimeSpan? KeepaliveCadence { get; set; }

    public sealed override TestingDbConnectionOptions GetConnectionOptions() =>
        new()
        { 
            ConnectionString = this.Db.ConnectionString, 
            ConnectionStringUseMultiplexing = this.UseMultiplexingNotTransaction == true,
            ConnectionStringUseTransaction = this.UseMultiplexingNotTransaction == false,
            ConnectionStringKeepaliveCadence = this.KeepaliveCadence,
        };

    public sealed override IDisposable? PrepareForHandleLost() => 
        new HandleLostScope(this.Db.SetUniqueApplicationName(nameof(this.PrepareForHandleLost)), this.Db);

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
    where TDb : TestingPrimaryClientDb, new()
{
    protected override bool? UseMultiplexingNotTransaction => true;
}

public sealed class TestingOwnedConnectionSynchronizationStrategy<TDb> : TestingConnectionStringSynchronizationStrategy<TDb>
    where TDb : TestingPrimaryClientDb, new()
{
    protected override bool? UseMultiplexingNotTransaction => null;
}

public sealed class TestingOwnedTransactionSynchronizationStrategy<TDb> : TestingConnectionStringSynchronizationStrategy<TDb>
    where TDb : TestingPrimaryClientDb, new()
{
    protected override bool? UseMultiplexingNotTransaction => false;
}

public abstract class TestingExternalConnectionOrTransactionSynchronizationStrategy<TDb> : TestingDbSynchronizationStrategy<TDb>
    where TDb : TestingDb, new()
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
    where TDb : TestingDb, new()
{
    private readonly DisposableCollection _disposables = new();
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

    public override ValueTask DisposeAsync()
    {
        this._disposables.Dispose();
        return base.DisposeAsync();
    }
}

public sealed class TestingExternalTransactionSynchronizationStrategy<TDb> : TestingExternalConnectionOrTransactionSynchronizationStrategy<TDb>
    where TDb : TestingDb, new()
{
    private readonly DisposableCollection _disposables = new();

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

    public override ValueTask DisposeAsync()
    {
        this._disposables.Dispose();
        return base.DisposeAsync();
    }
}
