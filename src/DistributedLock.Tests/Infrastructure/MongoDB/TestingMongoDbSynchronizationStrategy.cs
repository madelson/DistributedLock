using Medallion.Threading.Tests;
using Medallion.Threading.Internal;
using Medallion.Threading.MongoDB;
using MongoDB.Driver;

namespace Medallion.Threading.Tests.MongoDB;

public sealed class TestingMongoDbSynchronizationStrategy : TestingSynchronizationStrategy
{
    public Action? KillHandleAction { get; set; }

    public override void PrepareForHandleAbandonment()
    {
        this.KillHandleAction?.Invoke();
    }

    public override void PerformAdditionalCleanupForHandleAbandonment()
    {
        this.KillHandleAction?.Invoke();
    }

    public override IDisposable? PrepareForHandleLost()
    {
        return new DisposableAction(() => this.KillHandleAction?.Invoke());
    }

    private class DisposableAction(Action action) : IDisposable
    {
        private Action? _action = action;

        public void Dispose()
        {
            Interlocked.Exchange(ref this._action, null)?.Invoke();
        }
    }
}