using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal
{
    /// <summary>
    /// Similar to finalization, but allows for arbitrary managed code to be run for cleanup
    /// </summary>
#if DEBUG
    public
#else
    internal
#endif
    sealed class ManagedFinalizerQueue
    {
        // 99% of the time, the finalizer will do nothing because people will dispose properly. The finalizer also must
        // walk the full dictionary which in theory could be large if there is a lot of usage. Therefore, we don't want this to
        // run too frequently. On the other hand, when something does go wrong we want to be able to recover in some reasonable period
        // of time. 30s feels like is strikes a good balance here
        internal static readonly TimeSpan FinalizerCadence = TimeSpan.FromSeconds(
#if DEBUG
            3 // to keep tests fast, use a much shorter cadence in debug
#else
            30
#endif
        );

        public static readonly ManagedFinalizerQueue Instance = new ManagedFinalizerQueue();

        private readonly ConcurrentDictionary<IAsyncDisposable, WeakReference> _items = new ConcurrentDictionary<IAsyncDisposable, WeakReference>();

        // The state of this class can be described by 3 bits:
        // _count: >0 or ==0
        // _finalizerTask: has cleared initializing bit or has not cleared it
        // _initializing: 1 or 0
        //
        // The following shows the possible states we could be in:
        // _count   | _finalizerTask    | _initializing | nodes
        // 0        | cleared           | 0             | Initial state / finalizer getting ready to exit state. Register() => (>0, cleared, 1)
        // 0        | cleared           | 1             | If nothing changes, the finalizer will exit => (0, not cleared, 1). If something is registered => (>0, cleared, 1)
        // 0        | not cleared       | 0             | ERROR should never happen
        // 0        | not cleared       | 1             | Once our finalizer runs, it will => (0, cleared, 0). If something is registered => (>0, not cleared, 1)
        // >0       | cleared           | 0             | Finalizer is running. If count drops to zero => (0, cleared, 0)
        // >0       | cleared           | 1             | Means we dropped to 0 count but came back up before the finalizer exited. We now have a running finalizer and one queued
        // >0       | not cleared       | 0             | ERROR should never happen
        // >0       | not cleared       | 1             | Finalizer will run and transition to (>0, cleared, 0). Removal could transfer to (0, not cleared, 1)

        /// <summary>
        /// Tracked separately from the dictionary since (a) ConcurrentDictionary's count is slow and (b) we need to know exactly when we
        /// add one item to empty or remove one item from empty. We use a long to guarantee that we can't ever overflow (if there were really
        /// 2^63 items in the queue, we'd be out of memory)
        /// </summary>
        private long _count;

        private Task _finalizerTask = Task.CompletedTask;
        private int _finalizerTaskIsInitializing;

        private ManagedFinalizerQueue() { }

        /// <summary>
        /// If <paramref name="resource"/> is GC'd, <paramref name="finalizer"/> will be run.
        /// <paramref name="finalizer"/> must be thread-safe. Disposing the returned <see cref="IDisposable"/>
        /// revokes the registration. Note that, for this to work, <paramref name="finalizer"/> must not hold
        /// a strong reference to <paramref name="resource"/>.
        /// </summary>
        public IDisposable Register(object resource, IAsyncDisposable finalizer)
        {
            Invariant.Require(finalizer != resource);

            this._items.As<IDictionary<IAsyncDisposable, WeakReference>>()
                .Add(finalizer, new WeakReference(resource));
            
            if (Interlocked.Increment(ref this._count) == 1)
            {
                this.StartFinalizerTask();
            }

            return new Registration(this, finalizer);
        }

        private void StartFinalizerTask()
        {
            // If we're frequently adding and then removing a single item (probably a common case
            // since most of the time people will dispose things and won't do too much distributed locking),
            // we could end up thrashing where we create new finalizer tasks over and over again. To avoid
            // this, we set the initializing flag, but in the case that it was already set we know that there
            // is a task that is still getting ready; in that case we can just let that task continue to be
            // the finalizer task: there's no need to replace it
            if (Interlocked.Exchange(ref this._finalizerTaskIsInitializing, 1) != 0)
            {
                return;
            }

            // This lock is only barely necessary. The race condition this solves for is the continuation task
            // starting to run the finalizer loop AND clearing the initialization bit before we've assigned 
            // to this._finalizerTask. In that case, another thread could continue off the wrong task. The reason
            // this is super unlikely is because the loop sleeps before clearing the bit, so things have to go horribly
            // awry for this edge-case to occur.
            lock (this._items) // lock _items just because it's an object we own
            {
                // When we get here, the previous finalizer should exit on its next iteration but it hasn't
                // necessarily exited yet (and may not for some time). Therefore, we queue a task to run as a
                // continuation so that we only have one finalizer loop at a time
                this._finalizerTask = this._finalizerTask.ContinueWith(
                        (_, @this) => ((ManagedFinalizerQueue)@this).FinalizerLoop(),
                        state: this,
                        CancellationToken.None
                    )
                    .Unwrap();
            }
        }

        private async Task FinalizerLoop()
        {
            // Any new finalizer loop delays before doing anything else. We start the loop when we just added
            // something, so there's little chance of it having something to do right away
            await Task.Delay(FinalizerCadence).ConfigureAwait(false);

            // Clear the initializing flag. By doing this, we allow another task to be queued on top of us
            var initializingFlag = Interlocked.Exchange(ref this._finalizerTaskIsInitializing, 0);
            Invariant.Require(initializingFlag == 1);

            // Loop until there is nothing more to do
            while (Volatile.Read(ref this._count) != 0)
            {
                // the main finalizer does not wait for item finalization since we don't want that to ever
                // block or fault the main loop
                await this.FinalizeAsync(waitForItemFinalization: false).ConfigureAwait(false);
                await Task.Delay(FinalizerCadence).ConfigureAwait(false);
            }
        }

        private Task FinalizeAsync(bool waitForItemFinalization)
        {
            List<Task>? itemFinalizerTasks = null;

            // ConcurrentDictionary enumerator is safe to use concurrently with writes and is very inexpensive
            // (lock-free and does not generate a snapshot copy)
            foreach (var kvp in this._items)
            {
                if (!kvp.Value.IsAlive)
                {
                    var itemFinalizerTask = this.TryRemove(kvp.Key, disposeKey: true);
                    if (waitForItemFinalization)
                    {
                        (itemFinalizerTasks ??= new List<Task>()).Add(itemFinalizerTask);
                    }
                }
            }

            return waitForItemFinalization ? Task.WhenAll(itemFinalizerTasks ?? Enumerable.Empty<Task>()) : Task.CompletedTask;
        }

        /// <summary>
        /// Forces finalization of anything that is eligible. Exposed for testing purposes only
        /// </summary>
        internal Task FinalizeAsync() => this.FinalizeAsync(waitForItemFinalization: true);

        private Task TryRemove(IAsyncDisposable key, bool disposeKey)
        {
            if (this._items.TryRemove(key, out _))
            {
                Interlocked.Decrement(ref this._count);
                if (disposeKey)
                {
                    // DisposeAsync could throw, hang, etc. This must not block the finalizer thread.
                    // Therefore, we offload to a background thread and swallow exceptions
                    return Task.Run(() => key.DisposeAsync().AsTask());
                }
            }

            return Task.CompletedTask;
        }

        private sealed class Registration : IDisposable
        {
            private readonly ManagedFinalizerQueue _queue;
            private IAsyncDisposable? _key;

            public Registration(ManagedFinalizerQueue queue, IAsyncDisposable key)
            {
                this._queue = queue;
                this._key = key;
            }

            public void Dispose()
            {
                var key = Interlocked.Exchange(ref this._key, null);
                if (key != null)
                {
                    // If the registration gets disposed, we don't need to dispose the key
                    // because that means it got disposed normally
                    this._queue.TryRemove(key, disposeKey: false);
                }
            }
        }
    }
}
