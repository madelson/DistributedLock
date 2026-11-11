using Azure;
using Azure.Data.Tables;
using Medallion.Threading.Internal;

namespace Medallion.Threading.Azure;

public sealed partial class AzureTableDistributedLock
{
    internal sealed class InternalHandle : IDistributedSynchronizationHandle, LeaseMonitor.ILeaseHandle
    {
        private readonly AzureTableDistributedLock _lock;
        private readonly LeaseMonitor _leaseMonitor;

        /// <summary>
        /// Rotates on every renewal. This needs no synchronization because <see cref="LeaseMonitor.DisposeAsync"/>
        /// cancels and awaits the monitoring loop before <see cref="DisposeAsync"/> proceeds to the release,
        /// so renewal and release never overlap.
        /// </summary>
        private ETag _eTag;

        public InternalHandle(AzureTableDistributedLock @lock, string lockId, ETag eTag)
        {
            this._lock = @lock;
            this.LockId = lockId;
            this._eTag = eTag;
            this._leaseMonitor = new LeaseMonitor(this);
        }

        public string LockId { get; }

        public CancellationToken HandleLostToken => this._leaseMonitor.HandleLostToken;

        private bool RenewalEnabled => !this._lock._options.RenewalCadence.IsInfinite;

        TimeoutValue LeaseMonitor.ILeaseHandle.LeaseDuration => this._lock._options.Duration;

        TimeoutValue LeaseMonitor.ILeaseHandle.MonitoringCadence =>
            this.RenewalEnabled ? this._lock._options.RenewalCadence : this._lock._options.Duration;

        public void Dispose() => this.DisposeSyncViaAsync();

        public async ValueTask DisposeAsync()
        {
            // note that we're not trying to be idempotent here since we'll be wrapped
            // by AzureTableDistributedLockHandle which provides idempotence

            await this._leaseMonitor.DisposeAsync().ConfigureAwait(false);

            // Release by deleting, but only if we still own the row. A 412 or 404 means we already lost
            // the lease, in which case deleting would stomp on the new owner.
            try
            {
                await this._lock._tableClient
                    .DeleteEntityAsync(this._lock.PartitionKey, this._lock.RowKey, this._eTag, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (RequestFailedException ex) when (ex.Status is 412 or 404) { }
        }

        async Task<LeaseMonitor.LeaseState> LeaseMonitor.ILeaseHandle.RenewOrValidateLeaseAsync(CancellationToken cancellationToken)
        {
            var task = this.RenewalEnabled
                ? this.RenewAsync(cancellationToken)
                : this.ValidateAsync(cancellationToken);

            await task.TryAwait();
            cancellationToken.ThrowIfCancellationRequested(); // if cancellation caused failure, don't confuse that with losing the handle
            return task.Status == TaskStatus.RanToCompletion
                ? task.Result
                : LeaseMonitor.LeaseState.Unknown;
        }

        private async Task<LeaseMonitor.LeaseState> RenewAsync(CancellationToken cancellationToken)
        {
            var entity = this._lock.CreateEntity(this.LockId);
            try
            {
                var eTag = await this._lock._tableClient
                    .UpdateEntityAsync(entity, this._eTag, cancellationToken)
                    .ConfigureAwait(false);
                this._eTag = eTag;
                return LeaseMonitor.LeaseState.Renewed;
            }
            catch (RequestFailedException ex) when (ex.Status is 412 or 404)
            {
                return LeaseMonitor.LeaseState.Lost;
            }
        }

        private async Task<LeaseMonitor.LeaseState> ValidateAsync(CancellationToken cancellationToken)
        {
            // We compare LockId rather than ETag because an ETag mismatch cannot distinguish "someone else
            // took the lock" from a benign write, whereas LockId is unambiguous proof of ownership.
            var existing = await this._lock._tableClient
                .GetEntityIfExistsAsync<TableLockEntity>(this._lock.PartitionKey, this._lock.RowKey, cancellationToken)
                .ConfigureAwait(false);

            return existing.HasValue && existing.Value!.LockId == this.LockId
                ? LeaseMonitor.LeaseState.Held
                : LeaseMonitor.LeaseState.Lost;
        }
    }
}
