using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    /// <summary>
    /// An implementation of <see cref="IDistributedLock"/> which is SCOPED TO JUST THE CURRENT PROCESS and therefore
    /// is NOT TRULY DISTRIBUTED. Therefore, this implementation is intended primarily for testing or scenarios where
    /// name-based locking is useful (e.g. when frequently creating and destroying fine-grained locks).
    /// </summary>
    public sealed partial class ProcessScopedNamedLock : IInternalDistributedLock<ProcessScopedNamedLockHandle>
    {
        private static readonly NamedObjectPool<AsyncLockWrapper> NamedObjectPool = new(_ => new());

        /// <summary>
        /// Constructs a lock with <paramref name="name"/>.
        /// </summary>
        public ProcessScopedNamedLock(string name)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Implements <see cref="IDistributedLock.Name"/>
        /// </summary>
        public string Name { get; }

        async ValueTask<ProcessScopedNamedLockHandle?> IInternalDistributedLock<ProcessScopedNamedLockHandle>.InternalTryAcquireAsync(
            TimeoutValue timeout, 
            CancellationToken cancellationToken)
        {
            var acquired = false;
            var lease = NamedObjectPool.LeaseObject(this.Name);
            try
            {
                var handle = await lease.Value.Lock.TryAcquireAsync(timeout, cancellationToken).ConfigureAwait(false);
                if (handle is null) { return null; }

                acquired = true;
                return new(handle, lease);
            }
            finally
            {
                if (!acquired)
                {
                    lease.Dispose();
                }
            }
        }

        private sealed class AsyncLockWrapper
        {
            internal readonly AsyncLock Lock = AsyncLock.Create();
        }
    }
}
