using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    public sealed class TestingReaderWriterLockAsMutexProvider<TReaderWriterLockProvider, TStrategy> : TestingLockProvider<TStrategy>
        where TReaderWriterLockProvider : TestingReaderWriterLockProvider<TStrategy>, new()
        where TStrategy : TestingSynchronizationStrategy, new()
    {
        private readonly TReaderWriterLockProvider _readerWriterLockProvider = new TReaderWriterLockProvider();

        public override TStrategy Strategy => this._readerWriterLockProvider.Strategy;

        public override string GetCrossProcessLockType()
        {
            if (this._readerWriterLockProvider is ITestingUpgradeableReaderWriterLockProvider upgradeableProvider
                && GetShouldUseUpgradeLock())
            {
                return upgradeableProvider.GetCrossProcessLockType(ReaderWriterLockType.Upgrade);
            }

            return this._readerWriterLockProvider.GetCrossProcessLockType(ReaderWriterLockType.Write);
        }

        public override IDistributedLock CreateLockWithExactName(string name) => 
            new ReaderWriterLockAsMutex(this._readerWriterLockProvider.CreateReaderWriterLockWithExactName(name));

        public override string GetSafeName(string name) => this._readerWriterLockProvider.GetSafeName(name);

        public override void Dispose()
        {
            this._readerWriterLockProvider.Dispose();
            base.Dispose();
        }

        private static bool GetShouldUseUpgradeLock()
        {
            // intended to be random yet consistent across runs (assuming no changes)
            return (Environment.StackTrace.Length % 2) == 1;
        }

        private class ReaderWriterLockAsMutex : IDistributedLock
        {
            private readonly IDistributedReaderWriterLock _readerWriterLock;

            public ReaderWriterLockAsMutex(IDistributedReaderWriterLock readerWriterLock)
            {
                this._readerWriterLock = readerWriterLock;
            }

            string IDistributedLock.Name => this._readerWriterLock.Name;

            bool IDistributedLock.IsReentrant => this._readerWriterLock.IsReentrant;

            IDistributedLockHandle IDistributedLock.Acquire(TimeSpan? timeout, CancellationToken cancellationToken) =>
               this.ShouldUseUpgrade(out var upgradeable)
                    ? upgradeable.AcquireUpgradeableReadLock(timeout, cancellationToken)
                    : this._readerWriterLock.AcquireWriteLock(timeout, cancellationToken);

            ValueTask<IDistributedLockHandle> IDistributedLock.AcquireAsync(TimeSpan? timeout, CancellationToken cancellationToken) =>
                this.ShouldUseUpgrade(out var upgradeable)
                    ? upgradeable.AcquireUpgradeableReadLockAsync(timeout, cancellationToken).Convert(To<IDistributedLockHandle>.ValueTask)
                    : this._readerWriterLock.AcquireWriteLockAsync(timeout, cancellationToken);

            IDistributedLockHandle? IDistributedLock.TryAcquire(TimeSpan timeout, CancellationToken cancellationToken) =>
                this.ShouldUseUpgrade(out var upgradeable)
                    ? upgradeable.TryAcquireUpgradeableReadLock(timeout, cancellationToken)
                    : this._readerWriterLock.TryAcquireWriteLock(timeout, cancellationToken);

            ValueTask<IDistributedLockHandle?> IDistributedLock.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
                this.ShouldUseUpgrade(out var upgradeable)
                    ? upgradeable.TryAcquireUpgradeableReadLockAsync(timeout, cancellationToken).Convert(To<IDistributedLockHandle?>.ValueTask)
                    : this._readerWriterLock.TryAcquireWriteLockAsync(timeout, cancellationToken);

            private bool ShouldUseUpgrade(out IDistributedUpgradeableReaderWriterLock upgradeable)
            {
                if (this._readerWriterLock is IDistributedUpgradeableReaderWriterLock upgradeableLock
                    && GetShouldUseUpgradeLock())
                {
                    upgradeable = upgradeableLock;
                    return true;
                }

                upgradeable = null!;
                return false;
            }
        }
    }
}
