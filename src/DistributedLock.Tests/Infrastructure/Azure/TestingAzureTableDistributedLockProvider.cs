using Medallion.Threading.Azure;

namespace Medallion.Threading.Tests.Azure;

public sealed class TestingAzureTableDistributedLockProvider : TestingLockProvider<TestingAzureTableSynchronizationStrategy>
{
    public override IDistributedLock CreateLockWithExactName(string name) =>
        new AzureTableDistributedLock(
            this.Strategy.CreateTableClient(),
            name,
            o =>
            {
                this.Strategy.Options?.Invoke(o);
                // The name is already safe, so pass it through as the row key verbatim. This works only
                // because a custom selector's output is validated rather than sanitized.
                o.RowKey(AzureTableKeySelector.FromFunc(n => n));
                o.CreateTableIfNotExists();
            });

    public override string GetSafeName(string name) => AzureTableDistributedLock.GetSafeName(name);
}
