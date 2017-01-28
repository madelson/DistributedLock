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
                var pooledResult = MultiplexedConnectionLockPool.Get(this.connectionString).TryAcquire(this.lockName, mode);
                if (pooledResult.HasValue)
                {
                    // if we got a non-null value back, then we succeeded in using the shared connection. This
                    // result is valid if (a) it acquired the lock or (b) we weren't willing to wait anyway
                    // (since we never block on acquiring on the shared connection) AND we weren't looking to take
                    // a shared lock (if the pooled connection is already holding a shared version of the same lock,
                    // then it won't try to take that lock again. However, we might be able to take it on a different
                    // connection)

                    var handle = pooledResult.Value.Handle;
                    if (handle != null || (timeoutMillis == 0 && mode != SqlApplicationLock.Mode.Shared))
                    {
                        return handle;
                    }
                }
            }

            // otherwise, fall back to our fallback lock
            return this.fallbackLock.TryAcquire(timeoutMillis, mode, contextHandle);
        }

        public async Task<IDisposable> TryAcquireAsync(int timeoutMillis, SqlApplicationLock.Mode mode, CancellationToken cancellationToken, IDisposable contextHandle)
        {
            // manually check for cancellation since the shared lock does not support it
            cancellationToken.ThrowIfCancellationRequested();

            // cannot multiplex for updates, since we cannot predict whether or not there will be a request to elevate
            // to an exclusive lock which asks for a long timeout
            if (mode != SqlApplicationLock.Mode.Update && contextHandle == null)
            {
                var pooledResult = await MultiplexedConnectionLockPool.Get(this.connectionString)
                    .TryAcquireAsync(this.lockName, mode).ConfigureAwait(false);
                if (pooledResult.HasValue)
                {
                    // if we got a non-null value back, then we succeeded in using the shared connection. This
                    // result is valid if (a) it acquired the lock or (b) we weren't willing to wait anyway
                    // (since we never block on acquiring on the shared connection) AND we weren't looking to take
                    // a shared lock (if the pooled connection is already holding a shared version of the same lock,
                    // then it won't try to take that lock again. However, we might be able to take it on a different
                    // connection)

                    var handle = pooledResult.Value.Handle;
                    if (handle != null || (timeoutMillis == 0 && mode != SqlApplicationLock.Mode.Shared))
                    {
                        return handle;
                    }
                }
            }

            // otherwise, fall back to our fallback lock
            return await this.fallbackLock.TryAcquireAsync(timeoutMillis, mode, cancellationToken, contextHandle).ConfigureAwait(false);
        }
    }
}
