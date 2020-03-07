using Medallion.Threading.Data;
using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.SqlServer
{
    public abstract class SqlDistributedReaderWriterLockHandle : IDistributedLockHandle
    {
        // forbid external inheritors
        internal SqlDistributedReaderWriterLockHandle() { }

        public abstract CancellationToken HandleLostToken { get; }

        // todo should we have a common DisposeSyncOverAsync() extension for this?
        public void Dispose() => SyncOverAsync.Run(@this => @this.DisposeAsync(), this, willGoAsync: false);

        public abstract ValueTask DisposeAsync();
    }

    internal sealed class SqlDistributedReaderWriterLockNonUpgradeableHandle : SqlDistributedReaderWriterLockHandle
    {
        private IDistributedLockHandle? _innerHandle;

        internal SqlDistributedReaderWriterLockNonUpgradeableHandle(IDistributedLockHandle? handle)
        {
            this._innerHandle = handle;
        }

        public override CancellationToken HandleLostToken => this._innerHandle?.HandleLostToken ?? throw this.ObjectDisposed();

        public override ValueTask DisposeAsync() => Interlocked.Exchange(ref this._innerHandle, null)?.DisposeAsync() ?? default;
    }

    public sealed class SqlDistributedReaderWriterLockUpgradeableHandle : SqlDistributedReaderWriterLockHandle, IInternalDistributedLockUpgradeableHandle
    {
        private RefBox<(IDistributedLockHandle innerHandle, IInternalSqlDistributedLock @lock, IDistributedLockHandle? upgradedHandle)>? _box;

        internal SqlDistributedReaderWriterLockUpgradeableHandle(IDistributedLockHandle innerHandle, IInternalSqlDistributedLock @lock)
        {
            this._box = RefBox.Create((innerHandle, @lock, default(IDistributedLockHandle?)));
        }

        public override CancellationToken HandleLostToken => (this._box ?? throw this.ObjectDisposed()).Value.innerHandle.HandleLostToken;

        public override async ValueTask DisposeAsync()
        {
            if (RefBox.TryConsume(ref this._box, out var contents))
            {
                try { await (contents.upgradedHandle?.DisposeAsync() ?? default).ConfigureAwait(false); }
                finally { await contents.innerHandle.DisposeAsync().ConfigureAwait(false); }
            }
        }

        public bool TryUpgradeToWriteLock(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.TryUpgradeToWriteLock(this, timeout, cancellationToken);

        public ValueTask<bool> TryUpgradeToWriteLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            this.As<IInternalDistributedLockUpgradeableHandle>().InternalTryUpgradeToWriteLockAsync(timeout, cancellationToken);

        public void UpgradeToWriteLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.UpgradeToWriteLock(this, timeout, cancellationToken);

        public ValueTask UpgradeToWriteLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.UpgradeToWriteLockAsync(this, timeout, cancellationToken);

        async ValueTask<bool> IInternalDistributedLockUpgradeableHandle.InternalTryUpgradeToWriteLockAsync(TimeoutValue timeout, CancellationToken cancellationToken)
        {
            var box = this._box ?? throw this.ObjectDisposed();
            var contents = box.Value;
            if (contents.upgradedHandle != null) { throw new InvalidOperationException("the lock has already been upgraded"); }

            var upgradedHandle =
                await contents.@lock.TryAcquireAsync(timeout, SqlApplicationLock.ExclusiveLock, cancellationToken, contextHandle: contents.innerHandle).ConfigureAwait(false);
            if (upgradedHandle == null)
            {
                return false;
            }

            contents.upgradedHandle = upgradedHandle;
            var newBox = RefBox.Create(contents);
            if (Interlocked.CompareExchange(ref this._box, newBox, comparand: box) != box)
            {
                await upgradedHandle.DisposeAsync().ConfigureAwait(false);
            }

            return true;
        }
    }
}
