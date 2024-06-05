namespace Medallion.Threading.Tests;

public abstract class TestingReaderWriterLockProvider<TStrategy> : ITestingNameProvider, IAsyncDisposable
    where TStrategy : TestingSynchronizationStrategy, new()
{
    public TStrategy Strategy { get; } = new TStrategy();

    public abstract IDistributedReaderWriterLock CreateReaderWriterLockWithExactName(string name);
    public abstract string GetSafeName(string name);

    public virtual string GetCrossProcessLockType(ReaderWriterLockType type) =>
        type + this.CreateReaderWriterLock(string.Empty).GetType().Name;

    /// <summary>
    /// Returns a lock whose name is based on <see cref="TestingNameProviderExtensions.GetUniqueSafeName(ITestingNameProvider, string)"/>
    /// </summary>
    public IDistributedReaderWriterLock CreateReaderWriterLock(string baseName) =>
        this.CreateReaderWriterLockWithExactName(this.GetUniqueSafeName(baseName));

    public ValueTask DisposeAsync() => this.Strategy.DisposeAsync();
    public ValueTask SetupAsync() => this.Strategy.SetupAsync();
}

public abstract class TestingUpgradeableReaderWriterLockProvider<TStrategy> : TestingReaderWriterLockProvider<TStrategy>
    where TStrategy : TestingSynchronizationStrategy, new()
{
    public abstract IDistributedUpgradeableReaderWriterLock CreateUpgradeableReaderWriterLockWithExactName(string name);

    public sealed override IDistributedReaderWriterLock CreateReaderWriterLockWithExactName(string name) =>
        this.CreateUpgradeableReaderWriterLockWithExactName(name);

    /// <summary>
    /// Returns a lock whose name is based on <see cref="TestingNameProviderExtensions.GetUniqueSafeName(ITestingNameProvider, string)"/>
    /// </summary>
    public IDistributedUpgradeableReaderWriterLock CreateUpgradeableReaderWriterLock(string baseName) =>
        this.CreateUpgradeableReaderWriterLockWithExactName(this.GetUniqueSafeName(baseName));
}

public enum ReaderWriterLockType
{
    Read,
    Write,
    Upgrade,
}
