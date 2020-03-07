using Medallion.Threading.Internal;
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
        private readonly string _name, _connectionString;
        private readonly IInternalSqlDistributedLock _fallbackLock;

        public OptimisticConnectionMultiplexingSqlDistributedLock(string name, string connectionString)
        {
            this._name = name;
            this._connectionString = connectionString;
            this._fallbackLock = new OwnedConnectionSqlDistributedLock(name: name, connectionString: connectionString);
        }

        bool IInternalSqlDistributedLock.IsReentrant => false;

        public ValueTask<IDistributedLockHandle?> TryAcquireAsync<TLockCookie>(
            TimeoutValue timeout, 
            ISqlSynchronizationStrategy<TLockCookie> strategy, 
            CancellationToken cancellationToken, 
            IDistributedLockHandle? contextHandle)
            where TLockCookie : class
        {
            // cannot multiplex for updates, since we cannot predict whether or not there will be a request to elevate
            // to an exclusive lock which asks for a long timeout
            if (!strategy.IsUpgradeable && contextHandle == null)
            {
                return MultiplexedConnectionLockPool.Instance.TryAcquireAsync(this._connectionString, this._name, timeout, strategy, cancellationToken);
            }

            // otherwise, fall back to our fallback lock
            return this._fallbackLock.TryAcquireAsync(timeout, strategy, cancellationToken, contextHandle);
        }
    }
}
