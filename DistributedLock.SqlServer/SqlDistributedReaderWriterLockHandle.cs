using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.SqlServer
{
    /// <summary>
    /// Implements <see cref="IDistributedSynchronizationHandle"/>
    /// </summary>
    public abstract class SqlDistributedReaderWriterLockHandle : IDistributedSynchronizationHandle
    {
        // forbid external inheritors
        internal SqlDistributedReaderWriterLockHandle() { }

        /// <summary>
        /// Implements <see cref="IDistributedSynchronizationHandle.HandleLostToken"/>
        /// </summary>
        public abstract CancellationToken HandleLostToken { get; }

        /// <summary>
        /// Releases the lock
        /// </summary>
        public void Dispose() => this.DisposeSyncViaAsync();

        /// <summary>
        /// Releases the lock asynchronously
        /// </summary>
        public abstract ValueTask DisposeAsync();
    }

    internal sealed class SqlDistributedReaderWriterLockNonUpgradeableHandle : SqlDistributedReaderWriterLockHandle
    {
        private IDistributedSynchronizationHandle? _innerHandle;

        internal SqlDistributedReaderWriterLockNonUpgradeableHandle(IDistributedSynchronizationHandle? handle)
        {
            this._innerHandle = handle;
        }

        public override CancellationToken HandleLostToken => this._innerHandle?.HandleLostToken ?? throw this.ObjectDisposed();

        public override ValueTask DisposeAsync() => Interlocked.Exchange(ref this._innerHandle, null)?.DisposeAsync() ?? default;
    }

    /// <summary>
    /// Implements <see cref="IDistributedLockUpgradeableHandle"/>
    /// </summary>
    public sealed class SqlDistributedReaderWriterLockUpgradeableHandle : SqlDistributedReaderWriterLockHandle, IInternalDistributedLockUpgradeableHandle
    {
        private RefBox<(IDistributedSynchronizationHandle innerHandle, IDbDistributedLock @lock, IDistributedSynchronizationHandle? upgradedHandle)>? _box;

        internal SqlDistributedReaderWriterLockUpgradeableHandle(IDistributedSynchronizationHandle innerHandle, IDbDistributedLock @lock)
        {
            this._box = RefBox.Create((innerHandle, @lock, default(IDistributedSynchronizationHandle?)));
        }

        /// <summary>
        /// Implements <see cref="IDistributedSynchronizationHandle.HandleLostToken"/>
        /// </summary>
        public override CancellationToken HandleLostToken => (this._box ?? throw this.ObjectDisposed()).Value.innerHandle.HandleLostToken;

        /// <summary>
        /// Releases the lock asynchronously
        /// </summary>
        public override async ValueTask DisposeAsync()
        {
            if (RefBox.TryConsume(ref this._box, out var contents))
            {
                try { await (contents.upgradedHandle?.DisposeAsync() ?? default).ConfigureAwait(false); }
                finally { await contents.innerHandle.DisposeAsync().ConfigureAwait(false); }
            }
        }

        /// <summary>
        /// Implements <see cref="IDistributedLockUpgradeableHandle.TryUpgradeToWriteLock(TimeSpan, CancellationToken)"/>
        /// </summary>
        public bool TryUpgradeToWriteLock(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.TryUpgradeToWriteLock(this, timeout, cancellationToken);

        /// <summary>
        /// Implements <see cref="IDistributedLockUpgradeableHandle.TryUpgradeToWriteLockAsync(TimeSpan, CancellationToken)"/>
        /// </summary>
        public ValueTask<bool> TryUpgradeToWriteLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            this.As<IInternalDistributedLockUpgradeableHandle>().InternalTryUpgradeToWriteLockAsync(timeout, cancellationToken);

        /// <summary>
        /// Implements <see cref="IDistributedLockUpgradeableHandle.UpgradeToWriteLock(TimeSpan?, CancellationToken)"/>
        /// </summary>
        public void UpgradeToWriteLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.UpgradeToWriteLock(this, timeout, cancellationToken);

        /// <summary>
        /// Implements <see cref="IDistributedLockUpgradeableHandle.UpgradeToWriteLockAsync(TimeSpan?, CancellationToken)"/>
        /// </summary>
        public ValueTask UpgradeToWriteLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.UpgradeToWriteLockAsync(this, timeout, cancellationToken);

        ValueTask<bool> IInternalDistributedLockUpgradeableHandle.InternalTryUpgradeToWriteLockAsync(TimeoutValue timeout, CancellationToken cancellationToken)
        {
            var box = this._box ?? throw this.ObjectDisposed();
            var contents = box.Value;
            // todo ensure test for this
            if (contents.upgradedHandle != null) { throw new InvalidOperationException("the lock has already been upgraded"); }
            return TryPerformUpgradeAsync();

            async ValueTask<bool> TryPerformUpgradeAsync()
            {
                var upgradedHandle =
                    await contents.@lock.TryAcquireAsync(timeout, SqlApplicationLock.UpgradeLock, cancellationToken, contextHandle: contents.innerHandle).ConfigureAwait(false);
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
}
