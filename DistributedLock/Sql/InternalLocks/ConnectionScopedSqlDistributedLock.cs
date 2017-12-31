using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    internal sealed class ConnectionScopedSqlDistributedLock : IInternalSqlDistributedLock
    {
        private readonly string lockName;
        private readonly IDbConnection connection;

        public ConnectionScopedSqlDistributedLock(string lockName, IDbConnection connection)
        {
            this.lockName = lockName;
            this.connection = connection;
        }

        public IDisposable TryAcquire<TLockCookie>(int timeoutMillis, ISqlSynchronizationStrategy<TLockCookie> strategy, IDisposable contextHandle)
            where TLockCookie : class
        {
            this.CheckConnection();

            var lockCookie = strategy.TryAcquire(new ConnectionOrTransaction(this.connection), this.lockName, timeoutMillis);
            return lockCookie != null
                ? new LockScope<TLockCookie>(this, strategy, lockCookie)
                : null;
        }

        public async Task<IDisposable> TryAcquireAsync<TLockCookie>(int timeoutMillis, ISqlSynchronizationStrategy<TLockCookie> strategy, CancellationToken cancellationToken, IDisposable contextHandle)
            where TLockCookie : class
        {
            this.CheckConnection();

            var lockCookie = await strategy.TryAcquireAsync(new ConnectionOrTransaction(this.connection), this.lockName, timeoutMillis, cancellationToken).ConfigureAwait(false);
            return lockCookie != null
                ? new LockScope<TLockCookie>(this, strategy, lockCookie)
                : null;
        }

        private void CheckConnection()
        {
            if (this.connection.State != ConnectionState.Open)
                throw new InvalidOperationException("The connection is not open");
        }

        private void Release<TLockCookie>(ISqlSynchronizationStrategy<TLockCookie> strategy, TLockCookie lockCookie)
            where TLockCookie : class
        {
            if (this.connection.IsClosedOrBroken())
            {
                // lost the connection, so the lock was already released released
                return;
            }

            strategy.Release(new ConnectionOrTransaction(this.connection), this.lockName, lockCookie);
        }

        private sealed class LockScope<TLockCookie> : IDisposable
            where TLockCookie : class
        {
            private ConnectionScopedSqlDistributedLock @lock;
            private ISqlSynchronizationStrategy<TLockCookie> strategy;
            private TLockCookie lockCookie;

            public LockScope(ConnectionScopedSqlDistributedLock @lock, ISqlSynchronizationStrategy<TLockCookie> strategy, TLockCookie lockCookie)
            {
                this.@lock = @lock;
                this.strategy = strategy;
                this.lockCookie = lockCookie;
            }

            public void Dispose()
            {
                var @lock = Interlocked.Exchange(ref this.@lock, null);
                @lock?.Release(this.strategy, this.lockCookie);
                this.strategy = null;
                this.lockCookie = null;
            }
        }
    }
}
