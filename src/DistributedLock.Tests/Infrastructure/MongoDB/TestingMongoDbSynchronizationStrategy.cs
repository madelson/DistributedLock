namespace Medallion.Threading.Tests.MongoDB;

public sealed class TestingMongoDbSynchronizationStrategy : TestingSynchronizationStrategy
{
    public Action? KillHandleAction { get; set; }

    public override void PrepareForHandleAbandonment()
    {
        KillHandleAction?.Invoke();
    }
}