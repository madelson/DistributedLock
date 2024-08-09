﻿using Medallion.Threading.Internal;

namespace Medallion.Threading.Tests;

public abstract class TestingSemaphoreAsMutexProvider<TSemaphoreProvider, TStrategy> : TestingLockProvider<TStrategy>
    where TSemaphoreProvider : TestingSemaphoreProvider<TStrategy>, new()
    where TStrategy : TestingSynchronizationStrategy, new()
{
    private readonly TSemaphoreProvider _semaphoreProvider = new();
    private readonly DisposableCollection _disposables = new();
    private readonly HashSet<string> _mostlyDrainedSemaphoreNames = new();
    private readonly int _maxCount;

    protected TestingSemaphoreAsMutexProvider(int maxCount)
    {
        this._maxCount = maxCount;
    }

    public override TStrategy Strategy => this._semaphoreProvider.Strategy;

    public override string GetCrossProcessLockType() => $"{this._semaphoreProvider.GetCrossProcessLockType()}{this._maxCount}AsMutex";

    public override IDistributedLock CreateLockWithExactName(string name)
    {
        var semaphore = this._semaphoreProvider.CreateSemaphoreWithExactName(name, this._maxCount);
        lock (this._mostlyDrainedSemaphoreNames)
        {
            if (!this._mostlyDrainedSemaphoreNames.Contains(name))
            {
                this._mostlyDrainedSemaphoreNames.Add(name);

                // If our max count is > 1, we'll acquire the extra tickets such that any resolved semaphore
                // functions as a mutex
                for (var i = 0; i < this._maxCount - 1; ++i)
                {
                    this._disposables.Add(
                        semaphore.TryAcquire() 
                            ?? throw new InvalidOperationException($"Failed to take ticket {i} of {semaphore.GetType()} {name}")
                    );
                }
            }
        }

        return new SemaphoreAsMutex(semaphore);
    }

    public override string GetSafeName(string name) => this._semaphoreProvider.GetSafeName(name);

    public override async ValueTask DisposeAsync()
    {
        this._disposables.Dispose();
        await this._semaphoreProvider.DisposeAsync();
        await base.DisposeAsync();
    }

    private class SemaphoreAsMutex : IDistributedLock
    {
        private readonly IDistributedSemaphore _semaphore;

        public SemaphoreAsMutex(IDistributedSemaphore semaphore)
        {
            this._semaphore = semaphore;
        }

        string IDistributedLock.Name => this._semaphore.Name;

        IDistributedSynchronizationHandle IDistributedLock.Acquire(TimeSpan? timeout, CancellationToken cancellationToken) =>
            this._semaphore.Acquire(timeout, cancellationToken);

        ValueTask<IDistributedSynchronizationHandle> IDistributedLock.AcquireAsync(TimeSpan? timeout, CancellationToken cancellationToken) =>
            this._semaphore.AcquireAsync(timeout, cancellationToken).Convert(To<IDistributedSynchronizationHandle>.ValueTask);

        IDistributedSynchronizationHandle? IDistributedLock.TryAcquire(TimeSpan timeout, CancellationToken cancellationToken) =>
            this._semaphore.TryAcquire(timeout, cancellationToken);

        ValueTask<IDistributedSynchronizationHandle?> IDistributedLock.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            this._semaphore.TryAcquireAsync(timeout, cancellationToken).Convert(To<IDistributedSynchronizationHandle?>.ValueTask);
    }
}

[SupportsContinuousIntegration]
public sealed class TestingSemaphore1AsMutexProvider<TSemaphoreProvider, TStrategy> : TestingSemaphoreAsMutexProvider<TSemaphoreProvider, TStrategy>
    where TSemaphoreProvider : TestingSemaphoreProvider<TStrategy>, new()
    where TStrategy : TestingSynchronizationStrategy, new()
{
    public TestingSemaphore1AsMutexProvider() : base(maxCount: 1) { }
}

[SupportsContinuousIntegration]
public sealed class TestingSemaphore5AsMutexProvider<TSemaphoreProvider, TStrategy> : TestingSemaphoreAsMutexProvider<TSemaphoreProvider, TStrategy>
    where TSemaphoreProvider : TestingSemaphoreProvider<TStrategy>, new()
    where TStrategy : TestingSynchronizationStrategy, new()
{
    public TestingSemaphore5AsMutexProvider() : base(maxCount: 5) { }

    public override bool SupportsCrossProcessAbandonment => this.Strategy.SupportsCrossProcessSingleSemaphoreTicketAbandonment;
}
