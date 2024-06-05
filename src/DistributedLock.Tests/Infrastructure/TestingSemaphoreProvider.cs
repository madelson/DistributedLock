namespace Medallion.Threading.Tests;

public abstract class TestingSemaphoreProvider<TStrategy> : ITestingNameProvider, IAsyncDisposable
    where TStrategy : TestingSynchronizationStrategy, new()
{
    public TStrategy Strategy { get; } = new TStrategy();

    public abstract IDistributedSemaphore CreateSemaphoreWithExactName(string name, int maxCount);
    public abstract string GetSafeName(string name);

    public virtual string GetCrossProcessLockType() =>
        this.CreateSemaphore(string.Empty, maxCount: 1).GetType().Name;

    /// <summary>
    /// Returns a semaphore whose name is based on <see cref="TestingNameProviderExtensions.GetUniqueSafeName(ITestingNameProvider, string)"/>
    /// </summary>
    public IDistributedSemaphore CreateSemaphore(string baseName, int maxCount) =>
        this.CreateSemaphoreWithExactName(this.GetUniqueSafeName(baseName), maxCount);

    public ValueTask DisposeAsync() => this.Strategy.DisposeAsync();
    public ValueTask SetupAsync() => this.Strategy.SetupAsync();
}
