using Npgsql;
using NUnit.Framework;
using System.Data;

namespace Medallion.Threading.Tests.Postgres;

/// <summary>
/// This class contains tests which demonstrate specific Postgres/Npgsql behaviors which our implementations
/// rely on or account for. These should be tested through the normal set of test cases, but having this here 
/// is convenient as a demonstration / documentation
/// </summary>
public class PostgresBehaviorTest
{
    /// <summary>
    /// This test justifies why we do not need to have Postgres locks that take in a <see cref="System.Data.IDbTransaction"/>.
    /// Compare this behavior to <see cref="SqlServer.SqlDistributedLockTest.TestSqlCommandMustParticipateInTransaction"/>
    /// </summary>
    [Test]
    public async Task TestPostgresCommandAutomaticallyParticipatesInTransaction()
    {
        using var connection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await connection.OpenAsync();

        using var transaction =
#if NETCOREAPP
            await connection.BeginTransactionAsync();
#elif NETFRAMEWORK
            connection.BeginTransaction();
#endif

        using var commandInTransaction = connection.CreateCommand();
        commandInTransaction.Transaction = transaction;
        commandInTransaction.CommandText = @"SHOW statement_timeout; CREATE TABLE foo (id INT); SET LOCAL statement_timeout = 2020;";
        (await commandInTransaction.ExecuteScalarAsync()).ShouldEqual("0");

        using var commandOutsideTransaction = connection.CreateCommand();
        Assert.That(commandOutsideTransaction.Transaction, Is.Null);
        commandOutsideTransaction.CommandText = "SELECT COUNT(*) FROM foo";
        (await commandOutsideTransaction.ExecuteScalarAsync()).ShouldEqual(0);

        commandOutsideTransaction.CommandText = "SHOW statement_timeout";
        (await commandOutsideTransaction.ExecuteScalarAsync()).ShouldEqual("2020ms");

        commandInTransaction.CommandText = "SELECT COUNT(*) FROM foo";
        (await commandInTransaction.ExecuteScalarAsync()).ShouldEqual(0);

        commandInTransaction.CommandText = "SHOW statement_timeout";
        (await commandInTransaction.ExecuteScalarAsync()).ShouldEqual("2020ms");
    }

    [Test]
    public Task TestTransactionCancellationRecovery() =>
        this.TestTransactionCancellationOrTimeoutRecovery(useTimeout: false);

    [Test]
    public Task TestTransactionTimeoutRecovery() =>
        this.TestTransactionCancellationOrTimeoutRecovery(useTimeout: true);

    /// <summary>
    /// Demonstrates how we can leverage save points to recover from otherwise destroyed transactions
    /// </summary>
    private async Task TestTransactionCancellationOrTimeoutRecovery(bool useTimeout)
    {
        Assert.ThrowsAsync<PostgresException>(() => RunTransactionWithAbortAsync(useSavePoint: false));
        await RunTransactionWithAbortAsync(useSavePoint: true);

        async Task RunTransactionWithAbortAsync(bool useSavePoint)
        {
            using var connection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
            await connection.OpenAsync();

            using (connection.BeginTransaction())
            {
                var command = connection.CreateCommand();

                if (useSavePoint)
                {
                    command.CommandText = "SAVEPOINT cancellationRecovery";
                    await command.ExecuteNonQueryAsync();
                }

                command.CommandText = "SELECT pg_sleep(10)";
                using var cancellationTokenSource = new CancellationTokenSource();
                if (useTimeout) { command.CommandText = "SET LOCAL statement_timeout = 100; " + command.CommandText; }
                else { cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(.5)); }

                var exception = Assert.CatchAsync(() => command.ExecuteNonQueryAsync(cancellationTokenSource.Token));
                Assert.That(exception, Is.InstanceOf(useTimeout ? typeof(PostgresException) : typeof(OperationCanceledException)));

                if (useSavePoint)
                {
                    command.CommandText = "ROLLBACK TO SAVEPOINT cancellationRecovery";
                    await command.ExecuteNonQueryAsync();
                }

                command.CommandText = "SHOW statement_timeout";
                (await command.ExecuteScalarAsync()).ShouldEqual("0");
            }
        }
    }

    [Test]
    public async Task TestCanDetectTransactionWithBeginTransactionException()
    {
        using var connection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await connection.OpenAsync();

        Assert.DoesNotThrow(() => connection.BeginTransaction().Dispose());

        using var transaction = connection.BeginTransaction();

        var ex = Assert.Throws<InvalidOperationException>(() => connection.BeginTransaction().Dispose())!;
        Assert.That(ex.Message, Does.Contain("A transaction is already in progress"));
    }

    [Test]
    public async Task TestDoesNotDetectConnectionBreakViaState()
    {
        using var connection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await connection.OpenAsync();

        using var getPidCommand = connection.CreateCommand();
        getPidCommand.CommandText = "SELECT pg_backend_pid()";
        var pid = (int)(await getPidCommand.ExecuteScalarAsync())!;

        var stateChangedEvent = new ManualResetEventSlim(initialState: false);
        connection.StateChange += (_, _2) => stateChangedEvent.Set();

        // kill the connection from the back end
        using var killingConnection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await killingConnection.OpenAsync();
        using var killCommand = killingConnection.CreateCommand();
        killCommand.CommandText = $"SELECT pg_terminate_backend({pid})";
        await killCommand.ExecuteNonQueryAsync();

        Assert.That(stateChangedEvent.Wait(TimeSpan.FromSeconds(.1)), Is.False);

        // Catch rather than Throws because whether this surfaces as NpgsqlException (broken connection)
        // or the derived PostgresException (the server's 57P01 error message was read first) is timing-dependent
        Assert.Catch<NpgsqlException>(() => getPidCommand.ExecuteScalar());
        Assert.That(stateChangedEvent.Wait(TimeSpan.FromSeconds(5)), Is.True);
    }

    /// <summary>
    /// Demonstrates that a timed-out <see cref="NpgsqlConnection.WaitAsync(TimeSpan, CancellationToken)"/> is
    /// non-destructive: it returns false and the connection (including an open transaction) remains usable.
    /// Passive connection monitoring relies on this.
    /// </summary>
    [Test]
    public async Task TestWaitAsyncTimeoutDoesNotBreakConnection()
    {
        using var connection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await connection.OpenAsync();

        Assert.That(await connection.WaitAsync(TimeSpan.FromMilliseconds(100), CancellationToken.None), Is.False);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        (await command.ExecuteScalarAsync()).ShouldEqual(1);

        using (var transaction = connection.BeginTransaction())
        {
            Assert.That(await connection.WaitAsync(TimeSpan.FromMilliseconds(100), CancellationToken.None), Is.False);

            // the transaction was not aborted by the timed-out wait
            command.Transaction = transaction;
            command.CommandText = "SELECT 2";
            (await command.ExecuteScalarAsync()).ShouldEqual(2);
        }
    }

    /// <summary>
    /// Demonstrates that canceling <see cref="NpgsqlConnection.WaitAsync(TimeSpan, CancellationToken)"/> is
    /// non-destructive: it throws <see cref="OperationCanceledException"/> and the connection remains usable.
    /// Passive connection monitoring relies on this because the monitor's wait is canceled whenever the
    /// connection is needed for a real query.
    /// </summary>
    [Test]
    public async Task TestWaitAsyncCancellationDoesNotBreakConnection()
    {
        using var connection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await connection.OpenAsync();

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(.5));
        Assert.CatchAsync<OperationCanceledException>(() => connection.WaitAsync(TimeSpan.FromSeconds(30), cancellationTokenSource.Token));

        Assert.That(connection.State, Is.EqualTo(ConnectionState.Open));
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        (await command.ExecuteScalarAsync()).ShouldEqual(1);
    }

    /// <summary>
    /// Demonstrates that <see cref="NpgsqlConnection.WaitAsync(TimeSpan, CancellationToken)"/> returns true
    /// as soon as any message (e.g. a notification) arrives, before the timeout elapses. Passive connection
    /// monitoring accounts for this by looping until its full wait time has elapsed.
    /// </summary>
    [Test]
    public async Task TestWaitAsyncReturnsTrueWhenMessageArrives()
    {
        var channelName = $"wait_test_{Guid.NewGuid():N}";

        using var connection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await connection.OpenAsync();
        using (var listenCommand = connection.CreateCommand())
        {
            listenCommand.CommandText = $"LISTEN {channelName}";
            await listenCommand.ExecuteNonQueryAsync();
        }

        var waitTask = connection.WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None);

        using var notifyingConnection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await notifyingConnection.OpenAsync();
        using var notifyCommand = notifyingConnection.CreateCommand();
        notifyCommand.CommandText = $"NOTIFY {channelName}";
        await notifyCommand.ExecuteNonQueryAsync();

        Assert.That(await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromSeconds(10))), Is.SameAs(waitTask), "wait should complete when the notification arrives");
        Assert.That(await waitTask, Is.True);
    }

    /// <summary>
    /// Demonstrates that a connection killed during <see cref="NpgsqlConnection.WaitAsync(TimeSpan, CancellationToken)"/>
    /// throws and fires <see cref="System.Data.Common.DbConnection.StateChange"/>, which is what drives
    /// <see cref="IDistributedSynchronizationHandle.HandleLostToken"/> under passive monitoring.
    /// </summary>
    [Test]
    public async Task TestWaitAsyncOnKilledConnectionFiresStateChanged()
    {
        using var stateChangedEvent = new ManualResetEventSlim(initialState: false);

        using var connection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await connection.OpenAsync();
        connection.StateChange += (o, e) => stateChangedEvent.Set();

        using var getPidCommand = connection.CreateCommand();
        getPidCommand.CommandText = "SELECT pg_backend_pid()";
        var pid = (int)(await getPidCommand.ExecuteScalarAsync())!;

        var waitTask = connection.WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None);

        // kill the connection from the back end
        using var killingConnection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await killingConnection.OpenAsync();
        using var killCommand = killingConnection.CreateCommand();
        killCommand.CommandText = $"SELECT pg_terminate_backend({pid})";
        await killCommand.ExecuteNonQueryAsync();

        Assert.CatchAsync<NpgsqlException>(() => waitTask);
        Assert.That(connection.State, Is.Not.EqualTo(ConnectionState.Open));
        Assert.That(stateChangedEvent.Wait(TimeSpan.FromSeconds(5)), Is.True);
    }

    // Effective test for https://github.com/npgsql/npgsql/issues/3442, which broke monitoring
    [Test]
    public async Task TestExecutingQueryOnKilledConnectionFiresStateChanged()
    {
        using var stateChangedEvent = new ManualResetEventSlim(initialState: false);

        using var connection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await connection.OpenAsync();
        connection.StateChange += (o, e) => stateChangedEvent.Set();

        using var getPidCommand = connection.CreateCommand();
        getPidCommand.CommandText = "SELECT pg_backend_pid()";
        var pid = (int)(await getPidCommand.ExecuteScalarAsync())!;

        Assert.That(connection.State, Is.EqualTo(ConnectionState.Open));

        // kill the connection from the back end
        using var killingConnection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await killingConnection.OpenAsync();
        using var killCommand = killingConnection.CreateCommand();
        killCommand.CommandText = $"SELECT pg_terminate_backend({pid})";
        await killCommand.ExecuteNonQueryAsync();

        Assert.ThrowsAsync<PostgresException>(getPidCommand.ExecuteScalarAsync);
        Assert.That(connection.State, Is.Not.EqualTo(ConnectionState.Open));

        Assert.That(stateChangedEvent.Wait(TimeSpan.FromSeconds(5)), Is.True);
    }
}
