namespace Medallion.Threading.Tests;

/// <summary>
/// Manages the underlying approach to synchronization. Having this class allows us to parameterize tests by
/// synchronization strategy (e. g. only connection string-based strategies)
/// </summary>
public abstract class TestingSynchronizationStrategy : IAsyncDisposable
{
    /// <summary>
    /// Whether or not abandoning a ticket held in another process will cause that ticket
    /// to be released if tickets are still held elsewhere
    /// </summary>
    public virtual bool SupportsCrossProcessSingleSemaphoreTicketAbandonment => true;

    public virtual void PrepareForHandleAbandonment() { }
    public virtual void PerformAdditionalCleanupForHandleAbandonment() { }
    public virtual IDisposable? PrepareForHandleLost() => null;
    public virtual void PrepareForHighContention(ref int maxConcurrentAcquires) { }
    public virtual string GetConnectionStringForCrossProcessTest() => "<connection-string>";
    public virtual ValueTask SetupAsync() => ValueTask.CompletedTask;
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
