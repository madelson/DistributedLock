using Medallion.Threading.Sql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    public class SqlDistributedSemaphoreTestBase : DistributedLockTestBase
    {
        internal override IDistributedLock CreateLock(string name)
        {
            return new SqlDistributedSemaphore(name, maxCount: 1, connectionString: SqlDistributedLockTest.ConnectionString);
        }

        internal override string GetSafeLockName(string name) => name ?? throw new ArgumentNullException(name);
    }
}
