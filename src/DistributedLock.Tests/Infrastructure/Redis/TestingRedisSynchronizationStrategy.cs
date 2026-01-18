using Medallion.Threading.Internal;
using Medallion.Threading.Redis;

namespace Medallion.Threading.Tests.Redis;

public sealed class TestingRedisSynchronizationStrategy<TDatabaseProvider> : TestingSynchronizationStrategy
    where TDatabaseProvider : TestingRedisDatabaseProvider, new()
{
    private bool _preparedForHandleLost, _preparedForHandleAbandonment;
    private Action? _killHandleAction;
    private Action<RedisDistributedSynchronizationOptionsBuilder>? _options;

    public TDatabaseProvider DatabaseProvider { get; } = new TDatabaseProvider();

    public void SetOptions(Action<RedisDistributedSynchronizationOptionsBuilder>? options)
    {
        this._options = options;
    }

    public void Options(RedisDistributedSynchronizationOptionsBuilder options)
    {
        if (this._preparedForHandleLost)
        {
            options.ExtensionCadence(TimeSpan.FromMilliseconds(30));
        }
        if (this._preparedForHandleAbandonment)
        {
            options.Expiry(TimeSpan.FromSeconds(.2))
                // the reader writer lock requires that the busy wait sleep time is shorter
                // than the expiry, so adjust for that
                .BusyWaitSleepTime(TimeSpan.FromSeconds(.01), TimeSpan.FromSeconds(.1));    
        }

        this._options?.Invoke(options);
    }

    public override IDisposable? PrepareForHandleLost()
    {
        Invariant.Require(!this._preparedForHandleLost);
        this._preparedForHandleLost = true;
        return new ReleaseAction(() =>
        {
            Invariant.Require(this._preparedForHandleLost);
            try { this._killHandleAction?.Invoke(); }
            finally
            {
                this._killHandleAction = null;
                this._preparedForHandleLost = false;
            }
        });
    }

    public override void PrepareForHandleAbandonment() => this._preparedForHandleAbandonment = true;

    public override void PerformAdditionalCleanupForHandleAbandonment()
    {
        Invariant.Require(this._preparedForHandleAbandonment);
        Thread.Sleep(TimeSpan.FromSeconds(.5));
    }

    public void RegisterKillHandleAction(Action action)
    {
        if (this._preparedForHandleLost)
        {
            this._killHandleAction += action;
        }
    }
}
