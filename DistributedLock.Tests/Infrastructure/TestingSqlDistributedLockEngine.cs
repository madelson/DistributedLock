using Medallion.Threading.Sql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    public sealed class TestingSqlDistributedLockEngine<TConnectionManagementProvider> : TestingDistributedLockEngine
        where TConnectionManagementProvider : TestingSqlConnectionManagementProvider, new()
    {
        internal override IDistributedLock CreateLockWithExactName(string name)
        {
            var connectionManagementProvider = new TConnectionManagementProvider();
            this.RegisterCleanupAction(connectionManagementProvider.Dispose);

            var connectionInfo = connectionManagementProvider.GetConnectionInfo();
            if (connectionInfo.Strategy.HasValue)
            {
                return new SqlDistributedLock(name, connectionInfo.ConnectionString!, connectionInfo.Strategy.Value);
            }
            if (connectionInfo.ConnectionString != null)
            {
                return new SqlDistributedLock(name, connectionInfo.ConnectionString);
            }
            if (connectionInfo.Transaction != null)
            {
                return new SqlDistributedLock(name, connectionInfo.Transaction);
            }
            if (connectionInfo.Connection != null)
            {
                return new SqlDistributedLock(name, connectionInfo.Connection);
            }

            throw new NotSupportedException();
        }

        internal override bool IsReentrant
        {
            get
            {
                using var provider = new TConnectionManagementProvider();
                return provider.IsReentrantForAppLock;
            }
        }
        internal override string GetSafeLockName(string name) => SqlDistributedLock.GetSafeLockName(name);
        internal override void PerformCleanupForLockAbandonment()
        {
            using var provider = new TConnectionManagementProvider();
            provider.PerformCleanupForLockAbandonment();
        }
    }

    public sealed class TestingSqlDistributedLockEngineFactory : ITestingSqlDistributedLockEngineFactory
    {
        public TestingDistributedLockEngine Create<TConnectionManagementProvider>() where TConnectionManagementProvider : TestingSqlConnectionManagementProvider, new()
        {
            return new TestingSqlDistributedLockEngine<TConnectionManagementProvider>();
        }
    }
}
