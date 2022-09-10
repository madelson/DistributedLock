using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    /// <summary>
    /// An implementation of <see cref="IDistributedUpgradeableReaderWriterLock"/> which is SCOPED TO JUST THE CURRENT PROCESS and therefore
    /// is NOT TRULY DISTRIBUTED. Therefore, this implementation is intended primarily for testing or scenarios where
    /// name-based locking is useful (e.g. when frequently creating and destroying fine-grained locks).
    /// </summary>
    public sealed partial class ProcessScopedNamedReaderWriterLock
        : IInternalDistributedUpgradeableReaderWriterLock<ProcessScopedNamedReaderWriterLockHandle, ProcessScopedNamedReaderWriterLockUpgradeableHandle>
    {
        private static readonly NamedObjectPool<AsyncReaderWriterLock> NamedObjectPool = new(static _ => new());

        /// <summary>
        /// Constructs a lock with <paramref name="name"/>
        /// </summary>
        public ProcessScopedNamedReaderWriterLock(string name)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Implements <see cref="IDistributedReaderWriterLock.Name"/>
        /// </summary>
        public string Name { get; }

        async ValueTask<ProcessScopedNamedReaderWriterLockHandle?> IInternalDistributedReaderWriterLock<ProcessScopedNamedReaderWriterLockHandle>.InternalTryAcquireAsync(
            TimeoutValue timeout, 
            CancellationToken cancellationToken, 
            bool isWrite)
        {
            var result = await (
                isWrite ? this.TryAcquireAsync(static (l, t, c) => l.TryAcquireWriteLockAsync(t, c), timeout, cancellationToken)
                    : this.TryAcquireAsync(static (l, t, c) => l.TryAcquireReadLockAsync(t, c), timeout, cancellationToken)
            ).ConfigureAwait(false);
            return result is var (handle, lease)
                ? new ProcessScopedNamedReaderWriterLockNonUpgradeableHandle(handle, lease)
                : null;
        }

        async ValueTask<ProcessScopedNamedReaderWriterLockUpgradeableHandle?> IInternalDistributedUpgradeableReaderWriterLock<ProcessScopedNamedReaderWriterLockHandle, ProcessScopedNamedReaderWriterLockUpgradeableHandle>.InternalTryAcquireUpgradeableReadLockAsync(
            TimeoutValue timeout, 
            CancellationToken cancellationToken)
        {
            var result = await this.TryAcquireAsync(static (l, t, c) => l.TryAcquireUpgradeableReadLockAsync(t, c), timeout, cancellationToken).ConfigureAwait(false);
            return result is var (handle, lease)
                ? new(handle, lease)
                : null;
        }

        private async ValueTask<(THandle Handle, IDisposable Lease)?> TryAcquireAsync<THandle>(
            Func<AsyncReaderWriterLock, TimeoutValue, CancellationToken, ValueTask<THandle?>> tryAcquireAsync,
            TimeoutValue timeout, 
            CancellationToken cancellationToken)
            where THandle : class
        {
            var acquired = false;
            var lease = NamedObjectPool.LeaseObject(this.Name);
            try
            {
                var handle = await tryAcquireAsync(lease.Value, timeout, cancellationToken).ConfigureAwait(false);
                if (handle is null)
                {
                    return null;
                }

                acquired = true;
                return (handle, lease);
            }
            finally
            {
                if (!acquired)
                {
                    lease.Dispose();
                }
            }
        }
    }
}
