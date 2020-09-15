using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading.Internal;

namespace Medallion.Threading.Tests
{
    public abstract class TestingSemaphoreAsMutexProvider<TSemaphoreProvider, TStrategy> : TestingLockProvider<TStrategy>
        where TSemaphoreProvider : TestingSemaphoreProvider<TStrategy>, new()
        where TStrategy : TestingSynchronizationStrategy, new()
    {
        private readonly TSemaphoreProvider _semaphoreProvider = new TSemaphoreProvider();
        private readonly DisposableCollection _disposables = new DisposableCollection();
        private readonly HashSet<string> _mostlyDrainedSemaphoreNames = new HashSet<string>();
        private readonly int _maxCount;

        protected TestingSemaphoreAsMutexProvider(int maxCount)
        {
            this._maxCount = maxCount;
            this._disposables.Add(this._semaphoreProvider);
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

        public override void Dispose()
        {
            this._disposables.Dispose();
            base.Dispose();
        }

        private class SemaphoreAsMutex : IDistributedLock
        {
            private readonly IDistributedSemaphore _semaphore;

            public SemaphoreAsMutex(IDistributedSemaphore semaphore)
            {
                this._semaphore = semaphore;
            }

            string IDistributedLock.Name => throw new NotImplementedException();

            IDistributedLockHandle IDistributedLock.Acquire(TimeSpan? timeout, CancellationToken cancellationToken) =>
                this._semaphore.Acquire(timeout, cancellationToken);

            ValueTask<IDistributedLockHandle> IDistributedLock.AcquireAsync(TimeSpan? timeout, CancellationToken cancellationToken) =>
                this._semaphore.AcquireAsync(timeout, cancellationToken).Convert(To<IDistributedLockHandle>.ValueTask);

            IDistributedLockHandle? IDistributedLock.TryAcquire(TimeSpan timeout, CancellationToken cancellationToken) =>
                this._semaphore.TryAcquire(timeout, cancellationToken);

            ValueTask<IDistributedLockHandle?> IDistributedLock.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
                this._semaphore.TryAcquireAsync(timeout, cancellationToken).Convert(To<IDistributedLockHandle?>.ValueTask);
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
}
