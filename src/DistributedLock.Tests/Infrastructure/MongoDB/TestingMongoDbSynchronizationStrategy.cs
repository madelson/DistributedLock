using Medallion.Threading.Tests;
using Medallion.Threading.Internal;
using Medallion.Threading.MongoDB;
using MongoDB.Driver;

namespace Medallion.Threading.Tests.MongoDB;

public sealed class TestingMongoDbSynchronizationStrategy : TestingSynchronizationStrategy
{
    public Action? KillHandleAction { get; set; }

    public override void PrepareForHandleAbandonment() => this.KillHandleAction?.Invoke();

    public override void PerformAdditionalCleanupForHandleAbandonment() => this.KillHandleAction?.Invoke();

    public override IDisposable? PrepareForHandleLost() => new ReleaseAction(() => this.KillHandleAction?.Invoke());
}