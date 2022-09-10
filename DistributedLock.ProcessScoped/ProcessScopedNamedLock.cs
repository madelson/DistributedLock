using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    public sealed partial class ProcessScopedNamedLock : IInternalDistributedLock<ProcessScopedNamedLockHandle>
    {
        private static readonly NamedObjectPool<AsyncLockWrapper> NamedObjectPool = new(_ => new());

        public ProcessScopedNamedLock(string name)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
        }

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
