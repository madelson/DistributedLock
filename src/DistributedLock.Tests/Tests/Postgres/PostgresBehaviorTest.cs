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
        Assert.IsNull(commandOutsideTransaction.Transaction);
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
                Assert.IsInstanceOf(useTimeout ? typeof(PostgresException) : typeof(OperationCanceledException), exception);

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

        Assert.IsFalse(stateChangedEvent.Wait(TimeSpan.FromSeconds(.1)));

        Assert.Throws<NpgsqlException>(() => getPidCommand.ExecuteScalar());
        Assert.IsTrue(stateChangedEvent.Wait(TimeSpan.FromSeconds(5)));
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

        Assert.IsTrue(stateChangedEvent.Wait(TimeSpan.FromSeconds(5)));
    }
}
