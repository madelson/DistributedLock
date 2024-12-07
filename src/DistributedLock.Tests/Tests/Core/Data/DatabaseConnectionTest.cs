using Medallion.Threading.SqlServer;
using Medallion.Threading.Tests.SqlServer;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Core.Data;

public class DatabaseConnectionTest
{
    /// <summary>
    /// Reproduces the root cause of https://github.com/madelson/DistributedLock/issues/133
    /// </summary>
    [Test]
    public async Task TestConnectionMonitorStaysSubscribedAfterClose()
    {
        var db = new TestingSqlServerDb { ApplicationName = TestHelper.UniqueName };

        await using var connection = new SqlDatabaseConnection(db.ConnectionString);

        await connection.OpenAsync(CancellationToken.None);
        connection.ConnectionMonitor.GetMonitoringHandle().Dispose(); // initialize monitoring
        await connection.CloseAsync();

        await connection.OpenAsync(CancellationToken.None);
        using var handle = connection.ConnectionMonitor.GetMonitoringHandle();
        Assert.That(handle.ConnectionLostToken.IsCancellationRequested, Is.False);
        await db.KillSessionsAsync(db.ApplicationName, idleSince: null);
        Assert.That(await TestHelper.WaitForAsync(() => new(handle.ConnectionLostToken.IsCancellationRequested), timeout: TimeSpan.FromSeconds(5)), Is.True);
    }
}
