using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    public interface ITestingSqlDistributedLockEngineFactory
    {
        TestingDistributedLockEngine Create<TConnectionManagementProvider>()
            where TConnectionManagementProvider : TestingSqlConnectionManagementProvider, new();
    }
}
