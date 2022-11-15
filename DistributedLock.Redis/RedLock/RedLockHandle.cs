using Medallion.Threading.Internal;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Redis.RedLock;

internal sealed class RedLockHandle : IDistributedSynchronizationHandle, LeaseMonitor.ILeaseHandle
{
    private readonly IRedLockExtensibleSynchronizationPrimitive _primitive;
    private Dictionary<IDatabase, Task<bool>>? _tryAcquireTasks;
    private readonly TimeoutValue _extensionCadence, _expiry;
    private readonly LeaseMonitor _monitor;

    public RedLockHandle(
        IRedLockExtensibleSynchronizationPrimitive primitive,
        Dictionary<IDatabase, Task<bool>> tryAcquireTasks,
        TimeoutValue extensionCadence,
        TimeoutValue expiry)
    {
        this._primitive = primitive;
        this._tryAcquireTasks = tryAcquireTasks;
        this._extensionCadence = extensionCadence;
        this._expiry = expiry;
        // important to set this last, since the monitor constructor will read other fields of this
        this._monitor = new LeaseMonitor(this);
    }

    /// <summary>
    /// Implements <see cref="IDistributedSynchronizationHandle.HandleLostToken"/>
    /// </summary>
    public CancellationToken HandleLostToken => this._monitor.HandleLostToken;

    /// <summary>
    /// Releases the lock
    /// </summary>
    public void Dispose() => this.DisposeSyncViaAsync();

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

    TimeoutValue LeaseMonitor.ILeaseHandle.LeaseDuration => this._expiry;

    TimeoutValue LeaseMonitor.ILeaseHandle.MonitoringCadence => this._extensionCadence;

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
