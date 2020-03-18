using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading.Internal;

namespace Medallion.Threading.Tests
{
    public abstract class TestingSemaphoreAsMutexProvider<TSemaphoreProvider> : ITestingLockProvider
        where TSemaphoreProvider : ITestingSemaphoreProvider, new()
    {
        private readonly TSemaphoreProvider _semaphoreProvider = new TSemaphoreProvider();
        private readonly Dictionary<string, Stack<IDisposable>> _mostlyDrainedSemaphoreNames = new Dictionary<string, Stack<IDisposable>>();
        private readonly int _maxCount;

        protected TestingSemaphoreAsMutexProvider(int maxCount)
        {
            this._maxCount = maxCount;
        }

        public string CrossProcessLockType => nameof(SemaphoreAsMutex) + this._maxCount;

        public IDistributedLock CreateLockWithExactName(string name)
        {
            var semaphore = this._semaphoreProvider.CreateSemaphoreWithExactName(name, this._maxCount);
            lock (this._mostlyDrainedSemaphoreNames)
            {
                if (!this._mostlyDrainedSemaphoreNames.ContainsKey(name))
                {
                    var handles = new Stack<IDisposable>();
                    this._mostlyDrainedSemaphoreNames.Add(name, handles);
                    for (var i = 0; i < this._maxCount - 1; ++i)
                    {
                        handles.Push(
                            semaphore.TryAcquire() 
                                ?? throw new InvalidOperationException($"Failed to take ticket {i} of {semaphore.GetType()} {name}")
                        );
                    }
                }
            }

            return new SemaphoreAsMutex(semaphore);
        }

        public string GetSafeName(string name) => this._semaphoreProvider.GetSafeName(name);

        public void PerformAdditionalCleanupForHandleAbandonment() => this._semaphoreProvider.PerformAdditionalCleanupForHandleAbandonment();

        public void Dispose()
        {
            lock (this._mostlyDrainedSemaphoreNames)
            {
                var exceptions = new List<Exception>();
                foreach (var kvp in this._mostlyDrainedSemaphoreNames.ToArray())
                {
                    while (kvp.Value.Count != 0)
                    {
                        try { kvp.Value.Pop().Dispose(); }
                        catch (Exception ex) { exceptions.Add(ex); }
                    }
                    this._mostlyDrainedSemaphoreNames.Remove(kvp.Key);
                }

                try { this._semaphoreProvider.Dispose(); }
                catch (Exception ex) { exceptions.Add(ex); }

                if (exceptions.Any())
                {
                    throw new AggregateException(exceptions).Flatten();
                }
            }
        }

        private class SemaphoreAsMutex : IDistributedLock
        {
            private readonly Threading.SqlServer.SqlDistributedSemaphore _semaphore;

            public SemaphoreAsMutex(Threading.SqlServer.SqlDistributedSemaphore semaphore)
            {
                this._semaphore = semaphore;
            }

            string IDistributedLock.Name => throw new NotImplementedException();

            bool IDistributedLock.IsReentrant => throw new NotImplementedException();

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

    public sealed class TestingSemaphore1AsMutexProvider<TSemaphoreProvider> : TestingSemaphoreAsMutexProvider<TSemaphoreProvider>
        where TSemaphoreProvider : ITestingSemaphoreProvider, new()
    {
        public TestingSemaphore1AsMutexProvider() : base(maxCount: 1) { }
    }

    public sealed class TestingSemaphore5AsMutexProvider<TSemaphoreProvider> : TestingSemaphoreAsMutexProvider<TSemaphoreProvider>
        where TSemaphoreProvider : ITestingSemaphoreProvider, new()
    {
        public TestingSemaphore5AsMutexProvider() : base(maxCount: 5) { }
    }
}
