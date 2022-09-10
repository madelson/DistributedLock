using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    public abstract class ProcessScopedNamedReaderWriterLockHandle : IDistributedSynchronizationHandle
    {
        // forbid external inheritors
        internal ProcessScopedNamedReaderWriterLockHandle() { }

        CancellationToken IDistributedSynchronizationHandle.HandleLostToken => this.IsDisposed ? throw this.ObjectDisposed() : CancellationToken.None;

        private protected abstract bool IsDisposed { get; }

        public abstract void Dispose();

        public ValueTask DisposeAsync()
        {
            this.Dispose();
            return default;
        }
    }

    internal sealed class ProcessScopedNamedReaderWriterLockNonUpgradeableHandle : ProcessScopedNamedReaderWriterLockHandle
    {
        private HandleAndLease<IDisposable>? _handleAndLease;
        private IDisposable? _finalizerRegistration;

        public ProcessScopedNamedReaderWriterLockNonUpgradeableHandle(IDisposable handle, IDisposable lease)
        {
            this._handleAndLease = new(handle, lease);
            this._finalizerRegistration = ManagedFinalizerQueue.Instance.Register(this, this._handleAndLease);
        }

        private protected override bool IsDisposed => Volatile.Read(ref this._finalizerRegistration) is null;

        public override void Dispose()
        {
            Interlocked.Exchange(ref this._finalizerRegistration, null)?.Dispose();
            Interlocked.Exchange(ref this._handleAndLease, null)?.Dispose();
        }
    }

    public sealed class ProcessScopedNamedReaderWriterLockUpgradeableHandle : ProcessScopedNamedReaderWriterLockHandle, IInternalDistributedLockUpgradeableHandle
    {
        private HandleAndLease<AsyncReaderWriterLock.UpgradeableHandle>? _handleAndLease;
        private IDisposable? _finalizerRegistration;
        
        internal ProcessScopedNamedReaderWriterLockUpgradeableHandle(AsyncReaderWriterLock.UpgradeableHandle handle, IDisposable lease)
        {
            this._handleAndLease = new(handle, lease);
            this._finalizerRegistration = ManagedFinalizerQueue.Instance.Register(this, this._handleAndLease);
        }

        private protected override bool IsDisposed => Volatile.Read(ref this._finalizerRegistration) is null;

        public override void Dispose()
        {
            Interlocked.Exchange(ref this._handleAndLease, null)?.Dispose(); // call this first because it can throw if upgrading
            Interlocked.Exchange(ref this._finalizerRegistration, null)?.Dispose();
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
            // Note: we avoid locking here because we don't want to call TryUpgradeToWriteLockAsync inside the lock.
            // If the handle is disposed out from under us, then it will properly throw ObjectDisposedException.
            var handle = (Volatile.Read(ref this._handleAndLease) ?? throw this.ObjectDisposed()).LockHandle;
            return handle.TryUpgradeToWriteLockAsync(timeout, cancellationToken);
        }
    }
}
