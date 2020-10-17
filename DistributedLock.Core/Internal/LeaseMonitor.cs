using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal
{
    // todo incorporate into azure
    /// <summary>
    /// Utility for monitoring/renewing a fixed length "lease" lock
    /// </summary>
#if DEBUG
    public
#else
    internal
#endif
        sealed class LeaseMonitor : IDisposable, IAsyncDisposable
    {
        private readonly CancellationTokenSource _disposalSource = new CancellationTokenSource(),
            _handleLostSource = new CancellationTokenSource();
        
        private readonly WeakReference<ILeaseHandle> _weakLeaseHandle;
        private readonly TimeoutValue _leaseDuration, _monitoringCadence;
        private readonly Task _monitoringTask;

        public LeaseMonitor(ILeaseHandle handle)
        {
            Invariant.Require(handle.LeaseDuration.CompareTo(handle.MonitoringCadence) >= 0);

            this._weakLeaseHandle = new WeakReference<ILeaseHandle>(handle);
            this._leaseDuration = handle.LeaseDuration;
            this._monitoringCadence = handle.MonitoringCadence;
            this._monitoringTask = Task.Run(() => this.MonitoringLoop());
        }

        public CancellationToken HandleLostToken => this._handleLostSource.Token;

        public void Dispose() => SyncOverAsync.Run(@this => @this.DisposeAsync(), this);

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (!this._disposalSource.IsCancellationRequested) // idempotent
                {
                    this._disposalSource.Cancel();
                }

                if (SyncOverAsync.IsSynchronous) { this._monitoringTask.GetAwaiter().GetResult(); }
                else { await this._monitoringTask.ConfigureAwait(false); }
            }
            finally
            {
                this._handleLostSource.Dispose();
                this._disposalSource.Dispose();
            }
        }

        private async Task MonitoringLoop()
        {
            var leaseLifetime = Stopwatch.StartNew();
            while (true)
            {
                // wait until the next monitoring check
                await Task.Delay(this._monitoringCadence.InMilliseconds, this._disposalSource.Token).TryAwait();
                if (this._disposalSource.Token.IsCancellationRequested) { return; }

                // lease expired
                if (this._leaseDuration.CompareTo(leaseLifetime.Elapsed) < 0)
                {
                    this._handleLostSource.Cancel();
                    return;
                }

                var leaseState = await this.CheckLeaseAsync().ConfigureAwait(false);
                switch (leaseState)
                {
                    // if the handle is GC'd out from under us, then we just clean up after ourselves and exit
                    case null:
                        _ = Task.Run(() => this.Dispose()); // cleans up the sources. This can't be called synchronously because Dispose() waits on this task
                        return;

                    case LeaseState.Lost:
                        this._handleLostSource.Cancel();
                        return;

                    case LeaseState.Renewed:
                        leaseLifetime.Restart();
                        break;

                    // If the lease is held but not renewed or if we don't know (e. g. due to transient failure),
                    // then just continue. We can't yet say that it is lost but it isn't renewed so we can't reset
                    // the lifetime either.
                    case LeaseState.Held:
                    case LeaseState.Unknown:
                        break;

                    default:
                        throw new InvalidOperationException("should never get here");
                }
            }
        }

        private async Task<LeaseState?> CheckLeaseAsync()
        {
            if (!this._weakLeaseHandle.TryGetTarget(out var leaseHandle))
            {
                return null;
            }

            var renewOrValidateTask = Helpers.SafeCreateTask(state => state.leaseHandle.RenewOrValidateLeaseAsync(state.Token), (leaseHandle, this._disposalSource.Token));
            await renewOrValidateTask.TryAwait();
            return this._disposalSource.IsCancellationRequested || renewOrValidateTask.Status != TaskStatus.RanToCompletion
                ? LeaseState.Unknown
                : renewOrValidateTask.Result;
        }

        public interface ILeaseHandle
        {
            TimeoutValue LeaseDuration { get; }
            TimeoutValue MonitoringCadence { get; }
            Task<LeaseState> RenewOrValidateLeaseAsync(CancellationToken cancellationToken);
        }

        public enum LeaseState
        {
            /// <summary>
            /// The lease is known to be still held but was not renewed
            /// </summary>
            Held,

            /// <summary>
            /// The lease has been renewed for <see cref="ILeaseHandle.LeaseDuration"/>
            /// </summary>
            Renewed,

            /// <summary>
            /// The lease is known to no longer be held
            /// </summary>
            Lost,

            /// <summary>
            /// The lease may or may not be held any longer
            /// </summary>
            Unknown,
        }
    }
}
