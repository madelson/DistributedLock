using Medallion.Threading.Sql.ConnectionPooling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    internal sealed class OptimisticConnectionPoolingSqlDistributedLock : IInternalSqlDistributedLock
    {
        private readonly string lockName, connectionString;
        private readonly IInternalSqlDistributedLock fallbackLock;

        public OptimisticConnectionPoolingSqlDistributedLock(string lockName, string connectionString)
        {
            this.lockName = lockName;
            this.connectionString = connectionString;
            this.fallbackLock = new OwnedConnectionDistributedLock(lockName: lockName, connectionString: connectionString);
        }

        public IDisposable TryAcquire(int timeoutMillis)
        {
            var pooledResult = SharedConnectionLockPool.Get(this.connectionString).TryAcquire(this.lockName);
            if (pooledResult.HasValue)
            {
                // if we got a non-null value back, then we succeeded in using the shared connection. This
                // result is valid if (a) it acquired the lock or (b) we weren't willing to wait anyway
                // (since we never block on acquiring on the shared connection)

                var handle = pooledResult.Value.Handle;
                if (handle != null || timeoutMillis == 0)
                {
                    return handle;
                }
            }

            // otherwise, fall back to our fallback lock
            return this.fallbackLock.TryAcquire(timeoutMillis);
        }

        public async Task<IDisposable> TryAcquireAsync(int timeoutMillis, CancellationToken cancellationToken)
        {
            // manually check for cancellation since the shared lock does not support it
            cancellationToken.ThrowIfCancellationRequested();

            var pooledResult = await SharedConnectionLockPool.Get(this.connectionString)
                .TryAcquireAsync(this.lockName).ConfigureAwait(false);
            if (pooledResult.HasValue)
            {
                // if we got a non-null value back, then we succeeded in using the shared connection. This
                // result is valid if (a) it acquired the lock or (b) we weren't willing to wait anyway
                // (since we never block on acquiring on the shared connection)

                var handle = pooledResult.Value.Handle;
                if (handle != null || timeoutMillis == 0)
                {
                    return handle;
                }
            }

            // otherwise, fall back to our fallback lock
            return await this.fallbackLock.TryAcquireAsync(timeoutMillis, cancellationToken).ConfigureAwait(false);
        }
    }
}
