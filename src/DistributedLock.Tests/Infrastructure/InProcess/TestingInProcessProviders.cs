namespace Medallion.Threading.Tests.InProcess;

[SupportsContinuousIntegration]
public sealed class TestingInProcessDistributedLockProvider : TestingLockProvider<TestingInProcessSynchronizationStrategy>
{
    public override IDistributedLock CreateLockWithExactName(string name) =>
        this.Strategy.Provider.CreateLock(name);

    public override string GetSafeName(string name) => InProcessNameHelper.GetSafeName(name);
}

[SupportsContinuousIntegration]
public sealed class TestingInProcessDistributedSemaphoreProvider : TestingSemaphoreProvider<TestingInProcessSynchronizationStrategy>
{
    public override IDistributedSemaphore CreateSemaphoreWithExactName(string name, int maxCount) =>
        this.Strategy.Provider.CreateSemaphore(name, maxCount);

    public override string GetSafeName(string name) => InProcessNameHelper.GetSafeName(name);
}

[SupportsContinuousIntegration]
public sealed class TestingInProcessDistributedReaderWriterLockProvider : TestingUpgradeableReaderWriterLockProvider<TestingInProcessSynchronizationStrategy>
{
    public override IDistributedUpgradeableReaderWriterLock CreateUpgradeableReaderWriterLockWithExactName(string name) =>
        this.Strategy.Provider.CreateReaderWriterLock(name);

    public override string GetSafeName(string name) => InProcessNameHelper.GetSafeName(name);
}

internal static class InProcessNameHelper
{
    /// <summary>
    /// In-process names have no length or character restrictions, so any non-null name is already safe
    /// </summary>
    public static string GetSafeName(string name) => name ?? throw new ArgumentNullException(nameof(name));
}
