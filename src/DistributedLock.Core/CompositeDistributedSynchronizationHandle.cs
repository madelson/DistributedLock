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
            var existingBox = Volatile.Read(ref this._box);

            if (existingBox != null
                && existingBox.Value.HandleLostSource is null
                && CreateLinkedCancellationTokenSource(existingBox.Value.Handles) is { } handleLostSource)
            {
                var newContents = existingBox.Value;
                var newBox = RefBox.Create(newContents);
                var newExistingBox = Interlocked.CompareExchange(ref this._box, newBox, comparand: existingBox);
                if (newExistingBox != existingBox)
                {
                    handleLostSource.Dispose();
                    existingBox = newExistingBox;
                }
            }

            return existingBox is null ? throw this.ObjectDisposed() : existingBox.Value.HandleLostSource?.Token ?? CancellationToken.None;
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
        TimeSpan timeout,
        CancellationToken cancellationToken)
        where TPrimitive : class
    {
        if (primitives.Count == 1)
        {
            return new(await acquireFunc(primitives[0], timeout, cancellationToken) ?? primitives[0].As<object>());
        }

        var timeoutTracker = new TimeoutTracker(new TimeoutValue(timeout));
        var handles = new List<IDistributedSynchronizationHandle>(primitives.Count);
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
}