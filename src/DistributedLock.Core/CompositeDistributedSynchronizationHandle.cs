using Medallion.Threading.Internal;

namespace Medallion.Threading;

internal sealed class CompositeDistributedSynchronizationHandle : IDistributedSynchronizationHandle
{
    private RefBox<(IReadOnlyList<IDistributedSynchronizationHandle> Handles, CancellationTokenSource? HandleLostSource)>? _box;

    private CompositeDistributedSynchronizationHandle(IReadOnlyList<IDistributedSynchronizationHandle> handles)
    {
        this._box = RefBox.Create((handles, default(CancellationTokenSource)));
    }

    public CancellationToken HandleLostToken
    {
        get
        {
            var currentBox = Volatile.Read(ref this._box);

            if (currentBox != null
                && currentBox.Value.HandleLostSource is null
                && CreateLinkedCancellationTokenSource(currentBox.Value.Handles) is { } newHandleLostSource)
            {
                var newBox = RefBox.Create(currentBox.Value with { HandleLostSource = newHandleLostSource });
                var result = Interlocked.CompareExchange(ref this._box, newBox, comparand: currentBox);
                if (result == currentBox) { currentBox = newBox; }
                else { newHandleLostSource.Dispose(); } // lost the race
            }

            return currentBox is null
                ? throw this.ObjectDisposed() 
                : (currentBox.Value.HandleLostSource?.Token ?? CancellationToken.None);
        }
    }

    public void Dispose() => this.DisposeSyncViaAsync();

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref this._box, null) is { } box)
        {
            try { await DisposeHandlesAsync(box.Value.Handles).ConfigureAwait(false); }
            finally { box.Value.HandleLostSource?.Dispose(); }
        }
    }

    public static IReadOnlyList<TPrimitive> FromNames<TPrimitive, TState>(IReadOnlyList<string> names, TState state, Func<TState, string, TPrimitive> create)
    {
        if (names is null) { throw new ArgumentNullException(nameof(names)); }
        if (names.Count == 0) { throw new ArgumentException("At least one lock name is required.", nameof(names)); }
        if (names.Contains(null)) { throw new ArgumentException("Must not contain null", nameof(names)); }

        return names.Select(n => create(state, n)).ToArray();
    }

    public static async ValueTask<AcquireResult> TryAcquireAllAsync<TPrimitive>(
        IReadOnlyList<TPrimitive> primitives,
        Func<TPrimitive, TimeSpan, CancellationToken, ValueTask<IDistributedSynchronizationHandle?>> acquireFunc,
        TimeoutValue timeout,
        CancellationToken cancellationToken)
        where TPrimitive : class
    {
        if (primitives.Count == 1)
        {
            return new(await acquireFunc(primitives[0], timeout.TimeSpan, cancellationToken).ConfigureAwait(false) ?? primitives[0].As<object>());
        }

        TimeoutTracker timeoutTracker = new(timeout);
        List<IDistributedSynchronizationHandle> handles = new(primitives.Count);
        CompositeDistributedSynchronizationHandle? result = null;

        try
        {
            foreach (var primitive in primitives)
            {
                var handle = await acquireFunc(primitive, timeoutTracker.Remaining, cancellationToken)
                    .ConfigureAwait(false);

                if (handle is null)
                {
                    return new(primitive); // failure
                }

                handles.Add(handle);
            }

            result = new(handles);
        }
        finally
        {
            if (result is null)
            {
                await DisposeHandlesAsync(handles).ConfigureAwait(false);
            }
        }

        return new(result);
    }

    public readonly struct AcquireResult(object handleOrFailedPrimitive)
    {
        public IDistributedSynchronizationHandle? GetHandleOrDefault() =>
            handleOrFailedPrimitive as IDistributedSynchronizationHandle;
        public IDistributedSynchronizationHandle Handle =>
            this.GetHandleOrDefault() ?? throw new TimeoutException($"Timed out acquiring '{this.GetFailedName()}'");

        private string GetFailedName() => handleOrFailedPrimitive switch
        {
            IDistributedLock @lock => @lock.Name,
            IDistributedReaderWriterLock @lock => @lock.Name,
            IDistributedSemaphore semaphore => semaphore.Name,
            _ => handleOrFailedPrimitive.ToString()!
        };
    }

    private static CancellationTokenSource? CreateLinkedCancellationTokenSource(IReadOnlyList<IDistributedSynchronizationHandle> handles)
    {
        var cancellableTokens = handles
            .Select(h => h.HandleLostToken)
            .Where(t => t.CanBeCanceled)
            .ToArray();

        return cancellableTokens.Length > 0
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellableTokens)
            : null;
    }

    private static async ValueTask DisposeHandlesAsync(IReadOnlyList<IDistributedSynchronizationHandle> handles)
    {
        List<Exception>? exceptions = null;
        // release in reverse order of acquisition
        for (var i = handles.Count - 1; i >= 0; i--)
        {
            try
            {
                // in most cases Dispose() will call DisposeSyncViaAsync() anyway, but we need to do this to be
                // robust to externally-implemented handle types that aren't sync-via-async friendly
                if (SyncViaAsync.IsSynchronous) { handles[i].Dispose(); }
                else { await handles[i].DisposeAsync().ConfigureAwait(false); }
            }
            catch (Exception ex)
            {
                (exceptions ??= []).Add(ex);
            }
        }

        if (exceptions != null)
        {
            throw new AggregateException(exceptions);
        }
    }

    private readonly struct TimeoutTracker(TimeoutValue timeout)
    {
        private readonly System.Diagnostics.Stopwatch? _stopwatch = timeout.IsInfinite
            ? null
            : System.Diagnostics.Stopwatch.StartNew();

        public TimeSpan Remaining =>
            this._stopwatch is { Elapsed: var elapsed }
                ? elapsed >= timeout.TimeSpan
                    ? TimeSpan.Zero
                    : timeout.TimeSpan - elapsed
                : Timeout.InfiniteTimeSpan;
    }
}

internal static class CompositeDistributedLockHandleExtensions
{
    public static async ValueTask<IDistributedSynchronizationHandle?> GetHandleOrDefault(
        this ValueTask<CompositeDistributedSynchronizationHandle.AcquireResult> @this) =>
        (await @this.ConfigureAwait(false)).GetHandleOrDefault();

    public static async ValueTask<IDistributedSynchronizationHandle> GetHandleOrTimeout(
        this ValueTask<CompositeDistributedSynchronizationHandle.AcquireResult> @this) =>
        (await @this.ConfigureAwait(false)).Handle;

    public static ValueTask<CompositeDistributedSynchronizationHandle.AcquireResult> TryAcquireAllLocksInternalAsync(
        this IDistributedLockProvider provider,
        IReadOnlyList<string> names,
        TimeoutValue timeout,
        CancellationToken cancellationToken) =>
        CompositeDistributedSynchronizationHandle.TryAcquireAllAsync(
            CompositeDistributedSynchronizationHandle.FromNames(names, provider ?? throw new ArgumentNullException(nameof(provider)), static (p, n) => p.CreateLock(n)),
            static (p, t, c) => SyncViaAsync.IsSynchronous ? p.TryAcquire(t, c).AsValueTask() : p.TryAcquireAsync(t, c),
            timeout, cancellationToken);

    public static ValueTask<CompositeDistributedSynchronizationHandle.AcquireResult> TryAcquireAllReadLocksInternalAsync(
        this IDistributedReaderWriterLockProvider provider,
        IReadOnlyList<string> names,
        TimeoutValue timeout,
        CancellationToken cancellationToken) =>
        CompositeDistributedSynchronizationHandle.TryAcquireAllAsync(
            CompositeDistributedSynchronizationHandle.FromNames(
                names,
                provider ?? throw new ArgumentNullException(nameof(provider)),
                static (p, n) => p.CreateReaderWriterLock(n)),
            static (p, t, c) => SyncViaAsync.IsSynchronous ? p.TryAcquireReadLock(t, c).AsValueTask() : p.TryAcquireReadLockAsync(t, c),
            timeout, cancellationToken);

    public static ValueTask<CompositeDistributedSynchronizationHandle.AcquireResult> TryAcquireAllWriteLocksInternalAsync(
        this IDistributedReaderWriterLockProvider provider,
        IReadOnlyList<string> names,
        TimeoutValue timeout,
        CancellationToken cancellationToken) =>
        CompositeDistributedSynchronizationHandle.TryAcquireAllAsync(
            CompositeDistributedSynchronizationHandle.FromNames(
                names,
                provider ?? throw new ArgumentNullException(nameof(provider)),
                static (p, n) => p.CreateReaderWriterLock(n)),
            static (p, t, c) => SyncViaAsync.IsSynchronous ? p.TryAcquireWriteLock(t, c).AsValueTask() : p.TryAcquireWriteLockAsync(t, c),
            timeout, cancellationToken);

    public static ValueTask<CompositeDistributedSynchronizationHandle.AcquireResult> TryAcquireAllSemaphoresInternalAsync(
        this IDistributedSemaphoreProvider provider,
        IReadOnlyList<string> names,
        int maxCount,
        TimeoutValue timeout,
        CancellationToken cancellationToken) =>
        CompositeDistributedSynchronizationHandle.TryAcquireAllAsync(
            CompositeDistributedSynchronizationHandle.FromNames(
                names,
                (provider: provider ?? throw new ArgumentNullException(nameof(provider)), maxCount),
                static (s, n) => s.provider.CreateSemaphore(n, s.maxCount)),
            static (p, t, c) => SyncViaAsync.IsSynchronous ? p.TryAcquire(t, c).AsValueTask() : p.TryAcquireAsync(t, c),
            timeout,
            cancellationToken);
}