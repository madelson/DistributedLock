using Medallion.Threading.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.WaitHandles
{
    /// <summary>
    /// Implements <see cref="IDistributedSynchronizationHandle"/>
    /// </summary>
    public sealed class EventWaitHandleDistributedLockHandle : IDistributedSynchronizationHandle
    {
        private EventWaitHandle? _event;

        internal EventWaitHandleDistributedLockHandle(EventWaitHandle @event)
        {
            this._event = @event;
        }

        CancellationToken IDistributedSynchronizationHandle.HandleLostToken => 
            Volatile.Read(ref this._event) != null ? CancellationToken.None : throw this.ObjectDisposed();

        /// <summary>
        /// Releases the lock
        /// </summary>
        public void Dispose()
        {
            var @event = Interlocked.Exchange(ref this._event, null);
            if (@event != null)
            {
                @event.Set(); // signal
                @event.Dispose();
            }
        }

        /// <summary>
        /// Releases the lock asynchronously
        /// </summary>
        public ValueTask DisposeAsync()
        {
            this.Dispose();
            return default;
        }
    }
}
