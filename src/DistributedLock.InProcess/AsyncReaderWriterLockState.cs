using Medallion.Threading.Internal;

namespace Medallion.Threading.InProcess;

internal class AsyncReaderWriterLockState
{
    /// <summary>
    /// Held by writers for the duration of the lock; blocks incoming readers.
    /// </summary>
    private readonly SemaphoreSlim _entryGate = new(1, 1);
    /// <summary>
    /// Held by a writer, or collectively by the current set of readers (first in/last out).
    /// </summary>
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1);
    /// <summary>
    /// Held by the upgradeable reader and by writers. This makes upgradeable acquisitions mutually
    /// exclusive with each other and with writers, while still allowing plain readers.
    /// </summary>
    private readonly SemaphoreSlim _upgradeSemaphore = new(1, 1);
    /// <summary>
    /// Guards <see cref="_readerCount"/>. This is a plain monitor rather than a <see cref="SemaphoreSlim"/> because
    /// the release path must not block or go async: it runs from Dispose/DisposeAsync and from the
    /// <see cref="ManagedFinalizerQueue"/> thread. Every critical section it guards is non-blocking.
    /// </summary>
    private readonly object _readerCountLock = new();
    private int _readerCount = 0;
    /// <summary>
    /// The number of writers (including upgrading readers) that are waiting for or holding the write claim.
    /// Guarded by <see cref="_readerCountLock"/>. While this is zero, readers may bypass <see cref="_entryGate"/>.
    /// </summary>
    private int _writerCount = 0;

    internal ValueTask<bool> TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken, bool isWrite) =>
        this.TryAcquireAsync(timeout, cancellationToken, isWrite ? InProcessReaderWriterLockType.Write : InProcessReaderWriterLockType.Read);

    internal async ValueTask<bool> TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken, InProcessReaderWriterLockType type)
    {
        var tracker = new TimeoutTracker(timeout);

        switch (type)
        {
            case InProcessReaderWriterLockType.Upgrade:
                // an upgradeable reader only excludes other upgradeable readers and writers
                return await this._upgradeSemaphore.InternalWaitAsync(tracker.Remaining, cancellationToken).ConfigureAwait(false);

            case InProcessReaderWriterLockType.Write:
                if (!await this._upgradeSemaphore.InternalWaitAsync(tracker.Remaining, cancellationToken).ConfigureAwait(false))
                {
                    return false;
                }
                var acquiredWrite = false;
                try { return acquiredWrite = await this.TryAcquireWriteCoreAsync(tracker.Remaining, cancellationToken).ConfigureAwait(false); }
                finally { if (!acquiredWrite) { this._upgradeSemaphore.Release(); } }

            default:
                return await this.TryAcquireReadCoreAsync(tracker, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Acquires the write claim (<see cref="_entryGate"/> + <see cref="_writeSemaphore"/>). Assumes
    /// <see cref="_upgradeSemaphore"/> is already held by the caller. The inverse is <see cref="ReleaseWriteCore"/>.
    /// </summary>
    internal async ValueTask<bool> TryAcquireWriteCoreAsync(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        var tracker = new TimeoutTracker(timeout);

        // announce ourselves before touching the write semaphore so that readers stop taking the fast path
        lock (this._readerCountLock) { ++this._writerCount; }

        var acquired = false;
        try
        {
            // hold the entry gate for the duration of the write lock so that new readers queue up behind us
            if (!await this._entryGate.InternalWaitAsync(tracker.Remaining, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            try { return acquired = await this._writeSemaphore.InternalWaitAsync(tracker.Remaining, cancellationToken).ConfigureAwait(false); }
            finally { if (!acquired) { this._entryGate.Release(); } }
        }
        finally
        {
            if (!acquired) { lock (this._readerCountLock) { --this._writerCount; } }
        }
    }

    private async ValueTask<bool> TryAcquireReadCoreAsync(TimeoutTracker tracker, CancellationToken cancellationToken)
    {
        // Fast path: if no writer is waiting or holding, we can join the readers without going through the entry
        // gate. Every writer increments _writerCount under _readerCountLock before it tries to take _writeSemaphore,
        // so while _writerCount == 0 the write semaphore is either free or held by the current readers. This avoids
        // spurious zero-timeout failures caused by other readers briefly holding the entry gate.
        lock (this._readerCountLock)
        {
            if (this._writerCount == 0)
            {
                if (this._readerCount == 0)
                {
                    var acquiredWriteSemaphore = this._writeSemaphore.Wait(TimeSpan.Zero);
                    Invariant.Require(acquiredWriteSemaphore, "the write semaphore must be free when there are no readers or writers");
                }
                ++this._readerCount;
                return true;
            }
        }

        // Slow path: a writer is pending. Wait
        if (!await this._entryGate.InternalWaitAsync(tracker.Remaining, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        try
        {
            // We hold the entry gate, so no writer can hold _writeSemaphore right now. It is therefore either
            // free (_readerCount == 0) or held by the existing readers (_readerCount > 0). In the former case
            // the Wait below is guaranteed to succeed immediately, so this critical section never blocks.
            lock (this._readerCountLock)
            {
                if (this._readerCount == 0)
                {
                    var acquiredWriteSemaphore = this._writeSemaphore.Wait(TimeSpan.Zero);
                    Invariant.Require(acquiredWriteSemaphore, "the write semaphore must be free while the entry gate is held and there are no readers");
                }
                ++this._readerCount;
                return true;
            }
        }
        finally { this._entryGate.Release(); }
    }

    internal void Release(bool isWrite) =>
        this.Release(isWrite ? InProcessReaderWriterLockType.Write : InProcessReaderWriterLockType.Read, upgraded: false);

    internal void Release(InProcessReaderWriterLockType type, bool upgraded)
    {
        switch (type)
        {
            case InProcessReaderWriterLockType.Write:
                this.ReleaseWriteCore();
                this._upgradeSemaphore.Release();
                break;

            case InProcessReaderWriterLockType.Upgrade:
                if (upgraded) { this.ReleaseWriteCore(); }
                this._upgradeSemaphore.Release();
                break;

            default:
                // Non-blocking: see the remarks on _readerCountLock. This matters because release runs from
                // Dispose(), DisposeAsync() and the ManagedFinalizerQueue thread, none of which may block.
                lock (this._readerCountLock)
                {
                    if (--this._readerCount == 0) { this._writeSemaphore.Release(); }
                }
                break;
        }
    }

    /// <summary>
    /// Releases the write claim (<see cref="_writeSemaphore"/> + <see cref="_entryGate"/>) WITHOUT releasing
    /// <see cref="_upgradeSemaphore"/>. This is the inverse of <see cref="TryAcquireWriteCoreAsync"/>. It is used
    /// both by normal write release and to compensate for an upgrade that raced with handle disposal.
    /// </summary>
    internal void ReleaseWriteCore()
    {
        this._writeSemaphore.Release();
        this._entryGate.Release();
        // decrement last so that fast-path readers never observe _writerCount == 0 while we still hold _writeSemaphore
        lock (this._readerCountLock) { --this._writerCount; }
    }
}