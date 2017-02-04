using Medallion.Threading.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    [TestClass]
    public class SqlOwnedConnectionDistributedLockTest : DistributedLockTestBase
    {
        internal override IDistributedLock CreateLock(string name) 
            => new SqlDistributedLock(name, SqlDistributedLockTest.ConnectionString, SqlDistributedLockConnectionStrategy.Connection);

        internal override string GetSafeLockName(string name) => SqlDistributedLock.GetSafeLockName(name);
    }
}
