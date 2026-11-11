using Azure.Data.Tables;
using Medallion.Threading.Azure;
using NUnit.Framework;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Medallion.Threading.Tests.Azure;

public sealed class TestingAzureTableSynchronizationStrategy : TestingSynchronizationStrategy
{
    private readonly DisposableCollection _disposables = new();

    private static readonly Action<AzureTableOptionsBuilder> DefaultTestingOptions = o =>
        // for test speed
        o.BusyWaitSleepTime(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(25));

    public string TableName { get; set; } = AzureCredentials.DefaultTableName;

    public Action<AzureTableOptionsBuilder>? Options { get; set; } = DefaultTestingOptions;

    public TableClient CreateTableClient() => new(AzureCredentials.ConnectionString, this.TableName);

    public override IDisposable? PrepareForHandleLost()
    {
        this.Options = o =>
        {
            DefaultTestingOptions(o);
            o.RenewalCadence(TimeSpan.FromMilliseconds(10));
        };

        using var md5 = MD5.Create();
        // table names must be alphanumeric and begin with a letter
        this.TableName = $"dlhandlelost{new BigInteger(md5.ComputeHash(Encoding.UTF8.GetBytes(TargetFramework.Current + TestContext.CurrentContext.Test.FullName))):x}"
            .Replace("-", string.Empty);
        var tableClient = this.CreateTableClient();
        tableClient.CreateIfNotExists();
        this._disposables.Add(() => tableClient.Delete());
        return new HandleLostScope(this.TableName);
    }

    public override void PrepareForHighContention(ref int maxConcurrentAcquires)
    {
        // Under high contention the row almost always already exists, so probing with a read avoids
        // both a wasted request and a thrown RequestFailedException on every attempt.
        this.Options = o =>
        {
            DefaultTestingOptions(o);
            o.AcquireStrategy(AzureTableAcquireStrategy.ReadFirst);
        };
    }

    public override void Dispose()
    {
        try { this._disposables.Dispose(); }
        finally { base.Dispose(); }
    }

    private class HandleLostScope(string tableName) : IDisposable
    {
        public void Dispose()
        {
            // Deleting the lock rows out from under the holder causes its next renewal (which is
            // ETag-conditioned) to fail with a 404, which is how the handle is detected as lost.
            var tableClient = new TableClient(AzureCredentials.ConnectionString, tableName);
            foreach (var entity in tableClient.Query<TableEntity>())
            {
                tableClient.DeleteEntity(entity.PartitionKey, entity.RowKey);
            }
        }
    }
}
