using Medallion.Threading.InProcess;

namespace Medallion.Threading.Tests.InProcess;

[SupportsContinuousIntegration]
public sealed class TestingInProcessSynchronizationStrategy : TestingSynchronizationStrategy
{
    /// <summary>
    /// <see cref="InProcessDistributedSynchronizationProvider"/> caches its underlying primitives process-wide by scope,
    /// so each strategy uses a unique scope to isolate tests from one another. All synchronization primitives used by
    /// a single test must come from this provider.
    /// </summary>
    public InProcessDistributedSynchronizationProvider Provider { get; } = new(scope: Guid.NewGuid().ToString("N"));

    public override bool SupportsCrossProcess => false;
}
