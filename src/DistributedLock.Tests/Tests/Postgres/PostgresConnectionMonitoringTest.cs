using Medallion.Threading.Postgres;
using Medallion.Threading.Tests.Data;
using Npgsql;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Postgres;

/// <summary>
/// Tests for passive connection monitoring on Postgres: when <see cref="IDistributedSynchronizationHandle.HandleLostToken"/>
/// is used on an owned connection, monitoring uses <see cref="NpgsqlConnection.WaitAsync(TimeSpan, CancellationToken)"/>
/// (the session stays idle) rather than parking a pg_sleep query on the connection.
/// </summary>
public class PostgresConnectionMonitoringTest
{
    private readonly TestingPostgresDb _db = new();

    [Test]
    public async Task TestMonitoringSessionIsIdleWithoutSleepQuery()
    {
        var applicationName = UniqueApplicationName();
        var @lock = CreateLock(applicationName);
        await using var handle = await @lock.AcquireAsync();

        Assert.That(handle.HandleLostToken.CanBeCanceled, Is.True); // starts monitoring

        // give the monitoring worker time to engage
        await Task.Delay(TimeSpan.FromSeconds(1));

        using var connection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT state, query
            FROM pg_stat_activity
            WHERE application_name = @applicationName";
        command.Parameters.AddWithValue("applicationName", applicationName);

        var sessions = new List<(string State, string Query)>();
        using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                sessions.Add((
                    reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
                ));
            }
        }

        Assert.That(sessions, Is.Not.Empty);
        Assert.That(sessions, Has.All.Matches<(string State, string Query)>(s => s.State == "idle"), "monitored sessions should not be running a query");
        Assert.That(sessions, Has.None.Matches<(string State, string Query)>(s => s.Query.Contains("pg_sleep")), "monitoring should not use pg_sleep");
    }

    [Test]
    public async Task TestHandleLostTokenFiresOnKilledConnectionWithPassiveMonitoring()
    {
        var applicationName = UniqueApplicationName();
        var @lock = CreateLock(applicationName);
        var handle = await @lock.AcquireAsync();

        using var handleLostEvent = new ManualResetEventSlim(initialState: false);
        using var registration = handle.HandleLostToken.Register(handleLostEvent.Set);

        await this._db.KillSessionsAsync(applicationName, idleSince: null);

        Assert.That(handleLostEvent.Wait(TimeSpan.FromSeconds(10)), Is.True);

        // dispose may throw since the underlying connection is broken
        try { handle.Dispose(); } catch { }
    }

    [Test]
    [NonParallelizable, Retry(5)] // timing-sensitive
    public async Task TestMonitoringWithKeepaliveCadenceSurvivesIdleSessionKiller()
    {
        var applicationName = UniqueApplicationName();
        var @lock = CreateLock(applicationName, options => options.KeepaliveCadence(TimeSpan.FromSeconds(.05)));

        var handle = await @lock.AcquireAsync();
        Assert.That(handle.HandleLostToken.CanBeCanceled, Is.True); // monitoring + keepalive cadence => keepalive interleave

        using var idleSessionKiller = new IdleSessionKiller(this._db, applicationName, idleTimeout: TimeSpan.FromSeconds(.5));
        await Task.Delay(TimeSpan.FromSeconds(2));

        Assert.That(handle.HandleLostToken.IsCancellationRequested, Is.False);
        Assert.DoesNotThrow(handle.Dispose);
    }

    /// <summary>
    /// Npgsql KeepAlive is incompatible with canceling <see cref="NpgsqlConnection.WaitAsync(TimeSpan, CancellationToken)"/>,
    /// so monitoring falls back to the pg_sleep query for such connection strings. This verifies the fallback end-to-end.
    /// </summary>
    [Test]
    public async Task TestHandleLostTokenWorksWithNpgsqlKeepAliveFallback()
    {
        var applicationName = UniqueApplicationName();
        var @lock = CreateLock(applicationName, connectionStringOptions: builder => builder.KeepAlive = 1);
        var handle = await @lock.AcquireAsync();

        using var handleLostEvent = new ManualResetEventSlim(initialState: false);
        Assert.That(handle.HandleLostToken.CanBeCanceled, Is.True);
        using var registration = handle.HandleLostToken.Register(handleLostEvent.Set);

        await this._db.KillSessionsAsync(applicationName, idleSince: null);

        Assert.That(handleLostEvent.Wait(TimeSpan.FromSeconds(10)), Is.True);

        // dispose may throw since the underlying connection is broken
        try { handle.Dispose(); } catch { }
    }

    private static string UniqueApplicationName() => $"monitoring_test_{Guid.NewGuid():N}";

    private static PostgresDistributedLock CreateLock(
        string applicationName,
        Action<PostgresConnectionOptionsBuilder>? options = null,
        Action<NpgsqlConnectionStringBuilder>? connectionStringOptions = null)
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(TestingPostgresDb.DefaultConnectionString) { ApplicationName = applicationName };
        connectionStringOptions?.Invoke(connectionStringBuilder);

        // use a unique lock name since advisory lock keys are global to the database (and some tests retry)
        return new PostgresDistributedLock(new(Guid.NewGuid().ToString(), allowHashing: true), connectionStringBuilder.ConnectionString, options);
    }
}
