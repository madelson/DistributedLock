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
        return new HandleLostScope(this);
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

    public override string GetConnectionStringForCrossProcessTest() => this.DatabaseProvider.ConnectionStrings;

    public override ValueTask SetupAsync() => this.DatabaseProvider.SetupAsync();

    private class HandleLostScope : IDisposable
    {
        private TestingRedisSynchronizationStrategy<TDatabaseProvider>? _strategy;

        public HandleLostScope(TestingRedisSynchronizationStrategy<TDatabaseProvider> strategy)
        {
            this._strategy = strategy;
        }

        public void Dispose()
        {
            var strategy = Interlocked.Exchange(ref this._strategy, null);
            if (strategy != null)
            {
                Invariant.Require(strategy._preparedForHandleLost);
                try { strategy._killHandleAction?.Invoke(); }
                finally
                {
                    strategy._killHandleAction = null;
                    strategy._preparedForHandleLost = false;
                }
            }
        }
    }
}
