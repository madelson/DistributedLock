using Medallion.Threading.Internal;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.WaitHandles
{
    public sealed class EventWaitHandleDistributedLockHandle : IDistributedLockHandle
    {
        private EventWaitHandle? _event;

        internal EventWaitHandleDistributedLockHandle(EventWaitHandle @event)
        {
            this._event = @event;
        }

        CancellationToken IDistributedLockHandle.HandleLostToken => 
            Volatile.Read(ref this._event) != null ? CancellationToken.None : throw this.ObjectDisposed();

        public void Dispose()
        {
            var @event = Interlocked.Exchange(ref this._event, null);
            if (@event != null)
            {
                @event.Set(); // signal
                @event.Dispose();
            }
        }

        public ValueTask DisposeAsync()
        {
            this.Dispose();
            return default;
        }
    }
}
