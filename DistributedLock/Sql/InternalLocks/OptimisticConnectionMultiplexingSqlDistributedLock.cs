using Medallion.Threading.Sql.ConnectionMultiplexing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    internal sealed class OptimisticConnectionMultiplexingSqlDistributedLock : IInternalSqlDistributedLock
    {
        private static readonly MultiplexedConnectionLockPool LockPool = new MultiplexedConnectionLockPool();

        private readonly string lockName, connectionString;
        private readonly IInternalSqlDistributedLock fallbackLock;

        public OptimisticConnectionMultiplexingSqlDistributedLock(string lockName, string connectionString)
        {
            this.lockName = lockName;
            this.connectionString = connectionString;
            this.fallbackLock = new OwnedConnectionDistributedLock(lockName: lockName, connectionString: connectionString);
        }

        public IDisposable TryAcquire(int timeoutMillis, SqlApplicationLock.Mode mode, IDisposable contextHandle)
        {
            // cannot multiplex for updates, since we cannot predict whether or not there will be a request to elevate
            // to an exclusive lock which asks for a long timeout
            if (mode != SqlApplicationLock.Mode.Update && contextHandle == null)
            {
                return LockPool.TryAcquire(this.connectionString, this.lockName, timeoutMillis, mode);
            }

            // otherwise, fall back to our fallback lock
            return this.fallbackLock.TryAcquire(timeoutMillis, mode, contextHandle);
        }

        public Task<IDisposable> TryAcquireAsync(int timeoutMillis, SqlApplicationLock.Mode mode, CancellationToken cancellationToken, IDisposable contextHandle)
        {
            // cannot multiplex for updates, since we cannot predict whether or not there will be a request to elevate
            // to an exclusive lock which asks for a long timeout
            if (mode != SqlApplicationLock.Mode.Update && contextHandle == null)
            {
                return LockPool.TryAcquireAsync(connectionString, lockName, timeoutMillis, mode, cancellationToken);
            }

            // otherwise, fall back to our fallback lock
            return this.fallbackLock.TryAcquireAsync(timeoutMillis, mode, cancellationToken, contextHandle);
        }
    }
}
