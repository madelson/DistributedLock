using Medallion.Threading.Internal;
using Medallion.Threading.SqlServer;
using Medallion.Threading.Tests.Data;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.SqlServer
{
    public sealed class TestingSqlDistributedReaderWriterLockEngine<TConnectionManagementProvider> : TestingDistributedLockEngine
        where TConnectionManagementProvider : TestingSqlConnectionManagementProvider, new()
    {
        /// <summary>
        /// Used to provide repeatable randomization for <see cref="GetUseUpgradeLock(string)"/>
        /// </summary>
        private uint createCounter;

        internal override IDistributedLock CreateLockWithExactName(string name)
        {
            var readerWriterLock = this.CreateReaderWriterLockWithExactName(name);
            var @lock = new SqlDistributedReaderWriterLockLock(readerWriterLock, useUpgradeLock: this.GetUseUpgradeLock(name));
            ++this.createCounter;
            return @lock;
        }

        internal SqlDistributedReaderWriterLock CreateReaderWriterLock(string name) => 
            this.CreateReaderWriterLockWithExactName(this.GetUniqueSafeLockName(name));

        internal SqlDistributedReaderWriterLock CreateReaderWriterLockWithExactName(string name)
        {
            var connectionManagementProvider = new TConnectionManagementProvider();
            this.RegisterCleanupAction(connectionManagementProvider.Dispose);
            var connectionInfo = connectionManagementProvider.GetConnectionInfo();
            //if (connectionInfo.Strategy.HasValue)
            //{
            //    return new SqlDistributedReaderWriterLock(name, connectionInfo.ConnectionString!, connectionInfo.Strategy.Value);
            //}
            if (connectionInfo.ConnectionString != null)
            {
                return new SqlDistributedReaderWriterLock(name, connectionInfo.ConnectionString);
            }
            if (connectionInfo.Transaction != null)
            {
                return new SqlDistributedReaderWriterLock(name, connectionInfo.Transaction);
            }
            if (connectionInfo.Connection != null)
            {
                return new SqlDistributedReaderWriterLock(name, connectionInfo.Connection);
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// "Randomly" chooses whether to use an upgrade vs. a write lock to implement an exclusive lock
        /// </summary>
        private bool GetUseUpgradeLock(string name)
        {
            if (TestContext.CurrentContext.Test.FullName.Contains("Multiplex"))
            {
                // upgradeable locks cannot be multiplexed, so using them as exclusive locks in multiplexing tests
                // can subvert what those tests are trying to demonstrate. Therefore, we just use write locks in 
                // that case. Leveraging the name here is a bit hacky to be sure
                Console.WriteLine("Multiplexing test detected: forcing write lock");
                return false;
            }

            // consistent hash name:
            var hash = 0U;
            foreach (var ch in name)
            {
                hash = unchecked((31 * hash) + ch);
            }

            return ((hash ^ this.createCounter) & 1) == 0;
        }

        internal override bool IsReentrant
        {
            get
            {
                using var provider = new TConnectionManagementProvider();
                return provider.IsReentrantForAppLock;
            }
        }

        internal override string GetSafeName(string name) => SqlDistributedReaderWriterLock.GetSafeName(name);

        internal override void PerformCleanupForLockAbandonment()
        {
            using var provider = new TConnectionManagementProvider();
            provider.PerformCleanupForLockAbandonment();
        }

        private sealed class SqlDistributedReaderWriterLockLock : IDistributedLock
        {
            private readonly IDistributedUpgradeableReaderWriterLock @lock;
            private readonly bool useUpgradeLock;

            public SqlDistributedReaderWriterLockLock(SqlDistributedReaderWriterLock @lock, bool useUpgradeLock)
            {
                this.@lock = @lock;
                this.useUpgradeLock = useUpgradeLock;
            }

            public string Name => this.@lock.Name;

            public bool IsReentrant => this.@lock.IsReentrant;

            public IDistributedLockHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
                this.useUpgradeLock
                    ? this.@lock.AcquireUpgradeableReadLock(timeout, cancellationToken)
                    : this.@lock.AcquireWriteLock(timeout, cancellationToken);

            public ValueTask<IDistributedLockHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
                this.useUpgradeLock
                    ? this.@lock.AcquireUpgradeableReadLockAsync(timeout, cancellationToken).Convert(To<IDistributedLockHandle>.ValueTask)
                    : this.@lock.AcquireWriteLockAsync(timeout, cancellationToken);

            public IDistributedLockHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
                this.useUpgradeLock
                    ? this.@lock.TryAcquireUpgradeableReadLock(timeout, cancellationToken)
                    : this.@lock.TryAcquireWriteLock(timeout, cancellationToken);


            public ValueTask<IDistributedLockHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
                this.useUpgradeLock
                    ? this.@lock.TryAcquireUpgradeableReadLockAsync(timeout, cancellationToken).Convert(To<IDistributedLockHandle?>.ValueTask)
                    : this.@lock.TryAcquireWriteLockAsync(timeout, cancellationToken);
        }
    }

    public sealed class TestingSqlDistributedReaderWriterLockEngineFactory : ITestingSqlDistributedLockEngineFactory
    {
        public TestingDistributedLockEngine Create<TConnectionManagementProvider>() where TConnectionManagementProvider : TestingSqlConnectionManagementProvider, new()
        {
            return new TestingSqlDistributedReaderWriterLockEngine<TConnectionManagementProvider>();
        }
    }
}
