using DistributedLock.Tests;
using Medallion.Threading.Sql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    public abstract class TestingSqlConnectionManagementProvider : ActionRegistrationDisposable
    {
        public abstract ConnectionInfo GetConnectionInfo();

        /// <summary>
        /// See <see cref="TestingDistributedLockEngine.IsReentrant"/>
        /// </summary>
        internal abstract bool IsReentrantForAppLock { get; }

        /// <summary>
        /// See <see cref="TestingDistributedLockEngine.PerformCleanupForLockAbandonment"/>
        /// </summary>
        internal virtual void PerformCleanupForLockAbandonment()
        {
            // since connections are pooled, abandoning a SQL lock won't release the lock right away because the connection
            // simply releases back to the pool but doesn't receive an sp_resetconnection until it is re-opened or the pool
            // is cleared. Therefore, we clear the pool!

            SqlTestHelper.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    public class ConnectionInfo
    {
        public string? ConnectionString { get; set; }
        public SqlDistributedLockConnectionStrategy? Strategy { get; set; }
        public DbConnection? Connection { get; set; }
        public DbTransaction? Transaction { get; set; }
    }

    public interface IExternalConnectionOrTransactionTestingSqlConnectionManagementProvider { }
}
