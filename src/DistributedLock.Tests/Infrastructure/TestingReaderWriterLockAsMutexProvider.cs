﻿using Medallion.Threading.Internal;

namespace Medallion.Threading.Tests;

public interface ITestingReaderWriterLockAsMutexProvider
{
    public bool DisableUpgradeLock { get; set; }
}

public sealed class TestingReaderWriterLockAsMutexProvider<TReaderWriterLockProvider, TStrategy> : TestingLockProvider<TStrategy>, ITestingReaderWriterLockAsMutexProvider
    where TReaderWriterLockProvider : TestingReaderWriterLockProvider<TStrategy>, new()
    where TStrategy : TestingSynchronizationStrategy, new()
{
    private readonly TReaderWriterLockProvider _readerWriterLockProvider = new();

    public override TStrategy Strategy => this._readerWriterLockProvider.Strategy;

    public bool DisableUpgradeLock { get; set; }

    public override IDistributedLock CreateLockWithExactName(string name) => 
        new ReaderWriterLockAsMutex(this._readerWriterLockProvider.CreateReaderWriterLockWithExactName(name), this);

    public override string GetSafeName(string name) => this._readerWriterLockProvider.GetSafeName(name);

    public override string GetCrossProcessLockType() => 
        this._readerWriterLockProvider.GetCrossProcessLockType(ReaderWriterLockType.Write);

    public override async ValueTask DisposeAsync()
    {
        await this._readerWriterLockProvider.DisposeAsync();
        await base.DisposeAsync();
    }

    private bool GetShouldUseUpgradeLock()
    {
        return !this.DisableUpgradeLock
            // intended to be random yet consistent across runs (assuming no changes)
            && (Environment.StackTrace.Length % 2) == 1;
    }

    private class ReaderWriterLockAsMutex : IDistributedLock
    {
        private readonly TestingReaderWriterLockAsMutexProvider<TReaderWriterLockProvider, TStrategy> _provider;
        private readonly IDistributedReaderWriterLock _readerWriterLock;

        public ReaderWriterLockAsMutex(IDistributedReaderWriterLock readerWriterLock, TestingReaderWriterLockAsMutexProvider<TReaderWriterLockProvider, TStrategy> provider)
        {
            this._readerWriterLock = readerWriterLock;
            this._provider = provider;
        }

        string IDistributedLock.Name => this._readerWriterLock.Name;

        IDistributedSynchronizationHandle IDistributedLock.Acquire(TimeSpan? timeout, CancellationToken cancellationToken) =>
           this.ShouldUseUpgrade(out var upgradeable)
                ? upgradeable.AcquireUpgradeableReadLock(timeout, cancellationToken)
                : this._readerWriterLock.AcquireWriteLock(timeout, cancellationToken);

        ValueTask<IDistributedSynchronizationHandle> IDistributedLock.AcquireAsync(TimeSpan? timeout, CancellationToken cancellationToken) =>
            this.ShouldUseUpgrade(out var upgradeable)
                ? upgradeable.AcquireUpgradeableReadLockAsync(timeout, cancellationToken).Convert(To<IDistributedSynchronizationHandle>.ValueTask)
                : this._readerWriterLock.AcquireWriteLockAsync(timeout, cancellationToken);

        IDistributedSynchronizationHandle? IDistributedLock.TryAcquire(TimeSpan timeout, CancellationToken cancellationToken) =>
            this.ShouldUseUpgrade(out var upgradeable)
                ? upgradeable.TryAcquireUpgradeableReadLock(timeout, cancellationToken)
                : this._readerWriterLock.TryAcquireWriteLock(timeout, cancellationToken);

        ValueTask<IDistributedSynchronizationHandle?> IDistributedLock.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            this.ShouldUseUpgrade(out var upgradeable)
                ? upgradeable.TryAcquireUpgradeableReadLockAsync(timeout, cancellationToken).Convert(To<IDistributedSynchronizationHandle?>.ValueTask)
                : this._readerWriterLock.TryAcquireWriteLockAsync(timeout, cancellationToken);

        private bool ShouldUseUpgrade(out IDistributedUpgradeableReaderWriterLock upgradeable)
        {
            if (this._readerWriterLock is IDistributedUpgradeableReaderWriterLock upgradeableLock
                && this._provider.GetShouldUseUpgradeLock())
            {
                upgradeable = upgradeableLock;
                return true;
            }

            upgradeable = null!;
            return false;
        }
    }
}
