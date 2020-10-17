using Medallion.Threading.Internal;
using Medallion.Threading.Redis;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Medallion.Threading.Tests.Redis
{
    public sealed class TestingRedisSynchronizationStrategy : TestingSynchronizationStrategy
    {
        private bool _preparedForHandleLost, _preparedForHandleAbandonment;
        private Action? _killHandleAction;

        public void Options(RedisDistributedLockOptionsBuilder options)
        {
            if (this._preparedForHandleLost)
            {
                options.ExtensionCadence(TimeSpan.FromMilliseconds(30));
            }
            if (this._preparedForHandleAbandonment)
            {
                options.Expiry(TimeSpan.FromSeconds(.2));
            }
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

        private class HandleLostScope : IDisposable
        {
            private TestingRedisSynchronizationStrategy? _strategy;

            public HandleLostScope(TestingRedisSynchronizationStrategy strategy)
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
}
