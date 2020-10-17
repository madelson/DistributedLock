using Medallion.Threading.Internal;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Redis
{
    public sealed class RedisDistributedLockHandle : IDistributedLockHandle, LeaseMonitor.ILeaseHandle
    {
        private readonly IRedisSynchronizationPrimitive _primitive;
        private Dictionary<IDatabase, Task<bool>>? _tryAcquireTasks;
        private readonly LeaseMonitor _monitor;

        internal RedisDistributedLockHandle(
            IRedisSynchronizationPrimitive primitive,
            Dictionary<IDatabase, Task<bool>> tryAcquireTasks)
        {
            this._primitive = primitive;
            this._tryAcquireTasks = tryAcquireTasks;
            this._monitor = new LeaseMonitor(this);
        }

        /// <summary>
        /// Implements <see cref="IDistributedLockHandle.HandleLostToken"/>
        /// </summary>
        public CancellationToken HandleLostToken => this._monitor.HandleLostToken;

        /// <summary>
        /// Releases the lock
        /// </summary>
        public void Dispose() => SyncOverAsync.Run(@this => @this.DisposeAsync(), this);

        /// <summary>
        /// Releases the lock asynchronously
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await this._monitor.DisposeAsync().ConfigureAwait(false);
            var tryAcquireTasks = Interlocked.Exchange(ref this._tryAcquireTasks, null);
            if (tryAcquireTasks != null)
            {
                await new RedLockRelease(this._primitive, tryAcquireTasks).ReleaseAsync().ConfigureAwait(false);
            }
        }

        TimeoutValue LeaseMonitor.ILeaseHandle.LeaseDuration => this._primitive.Expiry;

        TimeoutValue LeaseMonitor.ILeaseHandle.MonitoringCadence => this._primitive.ExtensionCadence;

        async Task<LeaseMonitor.LeaseState> LeaseMonitor.ILeaseHandle.RenewOrValidateLeaseAsync(CancellationToken cancellationToken)
        {
            var extendResult = await new RedLockExtend(this._primitive, this._tryAcquireTasks!, cancellationToken).TryExtendAsync().ConfigureAwait(false);
            return extendResult switch
            {
                null => LeaseMonitor.LeaseState.Unknown,
                false => LeaseMonitor.LeaseState.Lost,
                true => LeaseMonitor.LeaseState.Renewed,
            };
        }
    }
}
