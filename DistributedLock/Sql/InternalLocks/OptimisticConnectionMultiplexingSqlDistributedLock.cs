using Medallion.Threading.Sql.ConnectionMultiplexing;
using System;
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
            this.fallbackLock = new OwnedConnectionSqlDistributedLock(lockName: lockName, connectionString: connectionString);
        }

        public IDisposable TryAcquire<TLockCookie>(int timeoutMillis, ISqlSynchronizationStrategy<TLockCookie> strategy, IDisposable contextHandle)
            where TLockCookie : class
        {
            // cannot multiplex for updates, since we cannot predict whether or not there will be a request to elevate
            // to an exclusive lock which asks for a long timeout
            if (!strategy.IsUpgradeable && contextHandle == null)
            {
                return MultiplexedConnectionLockPool.Instance.TryAcquire(this.connectionString, this.lockName, timeoutMillis, strategy);
            }

            // otherwise, fall back to our fallback lock
            return this.fallbackLock.TryAcquire(timeoutMillis, strategy, contextHandle);
        }

        public Task<IDisposable> TryAcquireAsync<TLockCookie>(int timeoutMillis, ISqlSynchronizationStrategy<TLockCookie> strategy, CancellationToken cancellationToken, IDisposable contextHandle)
            where TLockCookie : class
        {
            // cannot multiplex for updates, since we cannot predict whether or not there will be a request to elevate
            // to an exclusive lock which asks for a long timeout
            if (!strategy.IsUpgradeable && contextHandle == null)
            {
                return MultiplexedConnectionLockPool.Instance.TryAcquireAsync(connectionString, lockName, timeoutMillis, strategy, cancellationToken);
            }

            // otherwise, fall back to our fallback lock
            return this.fallbackLock.TryAcquireAsync(timeoutMillis, strategy, cancellationToken, contextHandle);
        }
    }
}
