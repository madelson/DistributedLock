using Medallion.Threading.Internal;

namespace Medallion.Threading;

internal sealed class CompositeDistributedSynchronizationHandle : IDistributedSynchronizationHandle
{
    private readonly IDistributedSynchronizationHandle[] _handles;
    private readonly CancellationTokenSource? _linkedLostCts;
    private bool _disposed;

    public CompositeDistributedSynchronizationHandle(IReadOnlyList<IDistributedSynchronizationHandle> handles)
    {
        ValidateHandles(handles);
        this._handles = handles.ToArray();
        this._linkedLostCts = this.CreateLinkedCancellationTokenSource();
    }

    public CancellationToken HandleLostToken => this._linkedLostCts?.Token ?? CancellationToken.None;

    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._disposed = true;
        var errors = this.DisposeHandles(h => h.Dispose());
        this._linkedLostCts?.Dispose();
        ThrowAggregateExceptionIfNeeded(errors, "disposing");
    }

    public async ValueTask DisposeAsync()
    {
        if (this._disposed)
        {
            return;
        }

        this._disposed = true;
        var errors = await this.DisposeHandlesAsync(h => h.DisposeAsync()).ConfigureAwait(false);
        this._linkedLostCts?.Dispose();
        ThrowAggregateExceptionIfNeeded(errors, "asynchronously disposing");
    }

    public static async ValueTask<IDistributedSynchronizationHandle?> TryAcquireAllAsync<TProvider>(
        TProvider provider,
        Func<TProvider, string, TimeSpan, CancellationToken, ValueTask<IDistributedSynchronizationHandle?>> acquireFunc,
        IReadOnlyList<string> names,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default)
    {
        ValidateAcquireParameters(provider, acquireFunc, names);

        var timeoutTracker = new TimeoutTracker(timeout);
        var handles = new List<IDistributedSynchronizationHandle>(names.Count);
        IDistributedSynchronizationHandle? result = null;

        try
        {
            foreach (var name in names)
            {
                var handle = await acquireFunc(provider, name, timeoutTracker.Remaining, cancellationToken)
                    .ConfigureAwait(false);

                if (handle is null)
                {
                    break;
                }

                handles.Add(handle);

                if (timeoutTracker.IsExpired)
                {
                    break;
                }
            }

            result = new CompositeDistributedSynchronizationHandle(handles);
        }
        finally
        {
            if (result is null)
            {
                await DisposeHandlesAsync(handles).ConfigureAwait(false);
            }
        }

        return result;
    }


    public static async ValueTask<IDistributedSynchronizationHandle> AcquireAllAsync<TProvider>(
        TProvider provider,
        Func<TProvider, string, TimeSpan, CancellationToken, ValueTask<IDistributedSynchronizationHandle>> acquireFunc,
        IReadOnlyList<string> names,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveTimeout = timeout ?? Timeout.InfiniteTimeSpan;
        var handle = await TryAcquireAllAsync(
                provider,
                WrapAcquireFunc(acquireFunc),
                names,
                effectiveTimeout,
                cancellationToken)
            .ConfigureAwait(false);

        if (handle is null)
        {
            throw new TimeoutException($"Timed out after {effectiveTimeout} while acquiring all locks.");
        }

        return handle;
    }

    public static IDistributedSynchronizationHandle? TryAcquireAll<TProvider>(
        TProvider provider,
        Func<TProvider, string, TimeSpan, CancellationToken, IDistributedSynchronizationHandle?> acquireFunc,
        IReadOnlyList<string> names,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(
            state => TryAcquireAllAsync(
                state.provider,
                WrapSyncAcquireFunc(state.acquireFunc),
                state.names,
                state.timeout,
                state.cancellationToken),
            (provider, acquireFunc, names, timeout, cancellationToken)
        );

    public static IDistributedSynchronizationHandle AcquireAll<TProvider>(
        TProvider provider,
        Func<TProvider, string, TimeSpan, CancellationToken, IDistributedSynchronizationHandle?> acquireFunc,
        IReadOnlyList<string> names,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(
            state => AcquireAllAsync(
                state.provider,
                WrapSyncAcquireFuncForRequired(state.acquireFunc),
                state.names,
                state.timeout,
                state.cancellationToken),
            (provider, acquireFunc, names, timeout, cancellationToken)
        );

    public static async ValueTask<IDistributedSynchronizationHandle?> TryAcquireAllAsync<TProvider>(
        TProvider provider,
        Func<TProvider, string, int, TimeSpan, CancellationToken, ValueTask<IDistributedSynchronizationHandle?>>
            acquireFunc,
        IReadOnlyList<string> names,
        int maxCount,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default)
    {
        ValidateAcquireParameters(provider, acquireFunc, names);

        var timeoutTracker = new TimeoutTracker(timeout);
        var handles = new List<IDistributedSynchronizationHandle>(names.Count);
        IDistributedSynchronizationHandle? result = null;

        try
        {
            foreach (var name in names)
            {
                var handle = await acquireFunc(provider, name, maxCount, timeoutTracker.Remaining, cancellationToken)
                    .ConfigureAwait(false);

                if (handle is null)
                {
                    break;
                }

                handles.Add(handle);

                if (timeoutTracker.IsExpired)
                {
                    break;
                }
            }

            result = new CompositeDistributedSynchronizationHandle(handles);
        }
        finally
        {
            if (result is null)
            {
                await DisposeHandlesAsync(handles).ConfigureAwait(false);
            }
        }

        return result;
    }


    public static async ValueTask<IDistributedSynchronizationHandle> AcquireAllAsync<TProvider>(
        TProvider provider,
        Func<TProvider, string, int, TimeSpan, CancellationToken, ValueTask<IDistributedSynchronizationHandle>>
            acquireFunc,
        IReadOnlyList<string> names,
        int maxCount,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveTimeout = timeout ?? Timeout.InfiniteTimeSpan;
        var handle = await TryAcquireAllAsync(
                provider,
                WrapAcquireFunc(acquireFunc),
                names,
                maxCount,
                effectiveTimeout,
                cancellationToken)
            .ConfigureAwait(false);

        if (handle is null)
        {
            throw new TimeoutException($"Timed out after {effectiveTimeout} while acquiring all locks.");
        }

        return handle;
    }

    public static IDistributedSynchronizationHandle? TryAcquireAll<TProvider>(
        TProvider provider,
        Func<TProvider, string, int, TimeSpan, CancellationToken, IDistributedSynchronizationHandle?> acquireFunc,
        IReadOnlyList<string> names,
        int maxCount,
        TimeSpan timeout = default,
        CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(
            state => TryAcquireAllAsync(
                state.provider,
                WrapSyncAcquireFunc(state.acquireFunc),
                state.names,
                state.maxCount,
                state.timeout,
                state.cancellationToken),
            (provider, acquireFunc, names, maxCount, timeout, cancellationToken)
        );

    public static IDistributedSynchronizationHandle AcquireAll<TProvider>(
        TProvider provider,
        Func<TProvider, string, int, TimeSpan, CancellationToken, IDistributedSynchronizationHandle?> acquireFunc,
        IReadOnlyList<string> names,
        int maxCount,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(
            state => AcquireAllAsync(
                state.provider,
                WrapSyncAcquireFuncForRequired(state.acquireFunc),
                state.names,
                state.maxCount,
                state.timeout,
                state.cancellationToken),
            (provider, acquireFunc, names, maxCount, timeout, cancellationToken)
        );

    private static void ValidateHandles(IReadOnlyList<IDistributedSynchronizationHandle> handles)
    {
        if (handles is null)
        {
            throw new ArgumentNullException(nameof(handles));
        }

        if (handles.Count == 0)
        {
            throw new ArgumentException("At least one handle is required", nameof(handles));
        }

        for (var i = 0; i < handles.Count; ++i)
        {
            if (handles[i] is null)
            {
                throw new ArgumentException(
                    $"Handles must not contain null elements; found null at index {i}",
                    nameof(handles)
                );
            }
        }
    }

    private CancellationTokenSource? CreateLinkedCancellationTokenSource()
    {
        var cancellableTokens = this._handles
            .Select(h => h.HandleLostToken)
            .Where(t => t.CanBeCanceled)
            .ToArray();

        return cancellableTokens.Length > 0
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellableTokens)
            : null;
    }

    private List<Exception>? DisposeHandles(Action<IDistributedSynchronizationHandle> disposeAction)
    {
        List<Exception>? errors = null;

        foreach (var handle in this._handles)
        {
            try
            {
                disposeAction(handle);
            }
            catch (Exception ex)
            {
                (errors ??= []).Add(ex);
            }
        }

        return errors;
    }

    private async ValueTask<List<Exception>?> DisposeHandlesAsync(
        Func<IDistributedSynchronizationHandle, ValueTask> disposeAction)
    {
        List<Exception>? errors = null;

        foreach (var handle in this._handles)
        {
            try
            {
                await disposeAction(handle).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                (errors ??= []).Add(ex);
            }
        }

        return errors;
    }

    private static void ThrowAggregateExceptionIfNeeded(List<Exception>? errors, string operation)
    {
        if (errors is not null && errors.Count > 0)
        {
            throw new AggregateException(
                $"One or more errors occurred while {operation} a composite distributed handle.", errors);
        }
    }

    private static void ValidateAcquireParameters<TProvider>(
        TProvider provider,
        Func<TProvider, string, TimeSpan, CancellationToken, ValueTask<IDistributedSynchronizationHandle?>> acquireFunc,
        IReadOnlyList<string> names)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        if (acquireFunc is null)
        {
            throw new ArgumentNullException(nameof(acquireFunc));
        }

        if (names is null)
        {
            throw new ArgumentNullException(nameof(names));
        }

        if (names.Count == 0)
        {
            throw new ArgumentException("At least one lock name is required.", nameof(names));
        }

        for (var i = 0; i < names.Count; ++i)
        {
            if (names[i] is null)
            {
                throw new ArgumentException(
                    $"Names must not contain null elements; found null at index {i}",
                    nameof(names)
                );
            }
        }
    }

    private static void ValidateAcquireParameters<TProvider>(
        TProvider provider,
        Func<TProvider, string, int, TimeSpan, CancellationToken, ValueTask<IDistributedSynchronizationHandle?>>
            acquireFunc,
        IReadOnlyList<string> names)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        if (acquireFunc is null)
        {
            throw new ArgumentNullException(nameof(acquireFunc));
        }

        if (names is null)
        {
            throw new ArgumentNullException(nameof(names));
        }

        if (names.Count == 0)
        {
            throw new ArgumentException("At least one lock name is required.", nameof(names));
        }

        for (var i = 0; i < names.Count; ++i)
        {
            if (names[i] is null)
            {
                throw new ArgumentException(
                    $"Names must not contain null elements; found null at index {i}",
                    nameof(names)
                );
            }
        }
    }

    private static async ValueTask DisposeHandlesAsync(List<IDistributedSynchronizationHandle> handles)
    {
        foreach (var handle in handles)
        {
            try
            {
                await handle.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                // Suppress exceptions during cleanup
            }
        }
    }

    private static Func<TProvider, string, TimeSpan, CancellationToken, ValueTask<IDistributedSynchronizationHandle?>>
        WrapAcquireFunc<TProvider>(
            Func<TProvider, string, TimeSpan, CancellationToken, ValueTask<IDistributedSynchronizationHandle>>
                acquireFunc) =>
        async (p, n, t, c) => await acquireFunc(p, n, t, c).ConfigureAwait(false);

    private static Func<TProvider, string, TimeSpan, CancellationToken, ValueTask<IDistributedSynchronizationHandle?>>
        WrapSyncAcquireFunc<TProvider>(
            Func<TProvider, string, TimeSpan, CancellationToken, IDistributedSynchronizationHandle?> acquireFunc) =>
        (p, n, t, c) => new ValueTask<IDistributedSynchronizationHandle?>(acquireFunc(p, n, t, c));

    private static Func<TProvider, string, TimeSpan, CancellationToken, ValueTask<IDistributedSynchronizationHandle>>
        WrapSyncAcquireFuncForRequired<TProvider>(
            Func<TProvider, string, TimeSpan, CancellationToken, IDistributedSynchronizationHandle?> acquireFunc) =>
        (p, n, t, c) =>
        {
            var handle = acquireFunc(p, n, t, c);
            return handle is not null
                ? new ValueTask<IDistributedSynchronizationHandle>(handle)
                : throw new TimeoutException($"Failed to acquire lock for '{n}'");
        };


    private static Func<TProvider, string, int, TimeSpan, CancellationToken,
            ValueTask<IDistributedSynchronizationHandle?>>
        WrapAcquireFunc<TProvider>(
            Func<TProvider, string, int, TimeSpan, CancellationToken, ValueTask<IDistributedSynchronizationHandle>>
                acquireFunc) =>
        async (p, n, mc, t, c) => await acquireFunc(p, n, mc, t, c).ConfigureAwait(false);

    private static Func<TProvider, string, int, TimeSpan, CancellationToken,
            ValueTask<IDistributedSynchronizationHandle?>>
        WrapSyncAcquireFunc<TProvider>(
            Func<TProvider, string, int, TimeSpan, CancellationToken, IDistributedSynchronizationHandle?>
                acquireFunc) =>
        (p, n, mc, t, c) => new ValueTask<IDistributedSynchronizationHandle?>(acquireFunc(p, n, mc, t, c));

    private static Func<TProvider, string, int, TimeSpan, CancellationToken,
            ValueTask<IDistributedSynchronizationHandle>>
        WrapSyncAcquireFuncForRequired<TProvider>(
            Func<TProvider, string, int, TimeSpan, CancellationToken, IDistributedSynchronizationHandle?>
                acquireFunc) =>
        (p, n, mc, t, c) =>
        {
            var handle = acquireFunc(p, n, mc, t, c);
            return handle is not null
                ? new ValueTask<IDistributedSynchronizationHandle>(handle)
                : throw new TimeoutException($"Failed to acquire lock for '{n}'");
        };

    private sealed class TimeoutTracker(TimeSpan timeout)
    {
        private readonly System.Diagnostics.Stopwatch? _stopwatch = timeout == Timeout.InfiniteTimeSpan
            ? null
            : System.Diagnostics.Stopwatch.StartNew();

        public TimeSpan Remaining => this._stopwatch is null
            ? Timeout.InfiniteTimeSpan
            : timeout - this._stopwatch.Elapsed;

        public bool IsExpired => this._stopwatch is not null && this._stopwatch.Elapsed >= timeout;
    }
}