namespace Medallion.Threading.Internal;

#if DEBUG
public
#else
internal
#endif 
readonly struct TimeoutTracker(TimeoutValue timeout)
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
