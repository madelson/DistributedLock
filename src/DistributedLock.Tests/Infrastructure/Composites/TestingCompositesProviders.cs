using Medallion.Threading.Tests.Data;
using Medallion.Threading.Tests.FileSystem;
using Medallion.Threading.Tests.Postgres;
using Medallion.Threading.Tests.WaitHandles;

namespace Medallion.Threading.Tests;

[SupportsContinuousIntegration]
public sealed class TestingCompositeDistributedLockProvider : TestingLockProvider<TestingLockFileSynchronizationStrategy>
{
    public override IDistributedLock CreateLockWithExactName(string name) => new TestingCompositeFileDistributedLock(name);

    public override string GetSafeName(string name) => name ?? throw new ArgumentNullException(nameof(name));
}

[SupportsContinuousIntegration(WindowsOnly = true)]
public sealed class TestingCompositeDistributedSemaphoreProvider : TestingSemaphoreProvider<TestingWaitHandleSynchronizationStrategy>
{
    public override IDistributedSemaphore CreateSemaphoreWithExactName(string name, int maxCount) =>
        new TestingCompositeWaitHandleDistributedSemaphore(name, maxCount);

    public override string GetSafeName(string name) => name ?? throw new ArgumentNullException(nameof(name));
}

public sealed class TestingCompositeReaderWriterLockProvider : TestingReaderWriterLockProvider<TestingConnectionMultiplexingSynchronizationStrategy<TestingPostgresDb>>
{
    public override IDistributedReaderWriterLock CreateReaderWriterLockWithExactName(string name) =>
        new TestingCompositePostgresReaderWriterLock(name, this.Strategy.GetConnectionOptions().ConnectionString!);

    public override string GetSafeName(string name) => name ?? throw new ArgumentNullException(nameof(name));
}