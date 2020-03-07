using Medallion.Threading.Internal;
using Medallion.Threading.SqlServer;
using Medallion.Threading.Tests.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.SqlServer
{
    public class TestingSqlDistributedSemaphoreEngine<TConnectionManagementProvider> : TestingDistributedLockEngine
        where TConnectionManagementProvider : TestingSqlConnectionManagementProvider, new()
    {
        private readonly HashSet<string> mostlyDrainedSemaphoreNames = new HashSet<string>();
        private readonly int maxCount;

        public TestingSqlDistributedSemaphoreEngine() : this(maxCount: 1) { }

        protected TestingSqlDistributedSemaphoreEngine(int maxCount)
        {
            this.maxCount = maxCount;
        }

        internal override IDistributedLock CreateLockWithExactName(string name)
        {
            var semaphore = this.CreateSemaphoreWithExactName(name, this.maxCount);

            // drain the semaphore to have 1 ticket remaining (making it a lock)
            lock (this.mostlyDrainedSemaphoreNames)
            {
                if (!this.mostlyDrainedSemaphoreNames.Contains(name))
                {
                    for (var i = 0; i < this.maxCount - 1; ++i)
                    {
                        var handle = semaphore.Acquire(TimeSpan.FromSeconds(30));
                        this.RegisterCleanupAction(handle.Dispose);
                    }
                    this.mostlyDrainedSemaphoreNames.Add(name);
                }
            }
            
            return new SqlDistributedSemaphoreLock(semaphore);
        }

        internal SqlDistributedSemaphore CreateSemaphore(string baseName, int maxCount) =>
            this.CreateSemaphoreWithExactName(this.GetUniqueSafeLockName(baseName), maxCount);

        private SqlDistributedSemaphore CreateSemaphoreWithExactName(string name, int maxCount)
        {
            var connectionManagementProvider = new TConnectionManagementProvider();
            this.RegisterCleanupAction(connectionManagementProvider.Dispose);
            var connectionInfo = connectionManagementProvider.GetConnectionInfo();
            if (connectionInfo.Strategy.HasValue)
            {
                return new SqlDistributedSemaphore(name, maxCount, connectionInfo.ConnectionString!, connectionInfo.Strategy.Value);
            }
            if (connectionInfo.ConnectionString != null)
            {
                return new SqlDistributedSemaphore(name, maxCount, connectionInfo.ConnectionString);
            }
            if (connectionInfo.Transaction != null)
            {
                return new SqlDistributedSemaphore(name, maxCount, connectionInfo.Transaction);
            }
            if (connectionInfo.Connection != null)
            {
                return new SqlDistributedSemaphore(name, maxCount, connectionInfo.Connection);
            }

            throw new NotSupportedException();
        }
        
        internal override bool IsReentrant => false;
        internal override string GetSafeName(string name) => name ?? throw new ArgumentNullException(nameof(name));
        internal override void PerformCleanupForLockAbandonment()
        {
            using var provider = new TConnectionManagementProvider();
            provider.PerformCleanupForLockAbandonment();
        }

        private sealed class SqlDistributedSemaphoreLock : IDistributedLock
        {
            private readonly SqlDistributedSemaphore _semaphore;

            public SqlDistributedSemaphoreLock(SqlDistributedSemaphore semaphore)
            {
                this._semaphore = semaphore;
            }

            string IDistributedLock.Name => this._semaphore.Name;

            bool IDistributedLock.IsReentrant => false;

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

    // note: we could make this class generic to test it with every connection strategy. However, there's little value to
    // retesting all connection modes again with the higher-ticket-count version
    public class TestingMostlyDrainedSqlSemaphoreDistributedLockEngine : TestingSqlDistributedSemaphoreEngine<DefaultConnectionStringProvider>
    {
        public TestingMostlyDrainedSqlSemaphoreDistributedLockEngine() : base(maxCount: 5) { }

        internal override string CrossProcessLockType => base.CrossProcessLockType + "5";
    }

    public sealed class TestingSqlDistributedSemaphoreEngineFactory : ITestingSqlDistributedLockEngineFactory
    {
        public TestingDistributedLockEngine Create<TConnectionManagementProvider>() where TConnectionManagementProvider : TestingSqlConnectionManagementProvider, new()
        {
            return new TestingSqlDistributedSemaphoreEngine<TConnectionManagementProvider>();
        }
    }
}
