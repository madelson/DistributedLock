using Medallion.Threading.Internal;

namespace Medallion.Threading.MongoDB;

/// <summary>
/// Options for configuring a MongoDB-based distributed synchronization algorithm
/// </summary>
public sealed class MongoDistributedSynchronizationOptionsBuilder
{
    private static readonly TimeoutValue DefaultExpiry = TimeSpan.FromSeconds(30);

    /// <summary>
    /// We don't want to allow expiry to go too low, since then the lock doesn't even work
    /// </summary>
    private static readonly TimeoutValue MinimumExpiry = TimeSpan.FromSeconds(.1);

    private TimeoutValue? _expiry, _extensionCadence, _minBusyWaitSleepTime, _maxBusyWaitSleepTime;

    private MongoDistributedSynchronizationOptionsBuilder() { }

    /// <summary>
    /// Specifies how long the lock will last, absent auto-extension. Because auto-extension exists,
    /// this value generally will have little effect on program behavior. However, making the expiry longer means that
    /// auto-extension requests can occur less frequently, saving resources. On the other hand, when a lock is abandoned
    /// without explicit release (e.g. if the holding process crashes), the expiry determines how long other processes
    /// would need to wait in order to acquire it.
    /// Defaults to 30s.
    /// </summary>
    public MongoDistributedSynchronizationOptionsBuilder Expiry(TimeSpan expiry)
    {
        var expiryTimeoutValue = new TimeoutValue(expiry, nameof(expiry));
        if (expiryTimeoutValue.IsInfinite || expiryTimeoutValue.CompareTo(MinimumExpiry) < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expiry), expiry, $"Must be >= {MinimumExpiry.TimeSpan} and < ∞");
        }
        _expiry = expiryTimeoutValue;
        return this;
    }

    /// <summary>
    /// Determines how frequently the lock will be extended while held. More frequent extension means more unnecessary requests
    /// but also a lower chance of losing the lock due to the process hanging or otherwise failing to get its extension request in
    /// before the lock expiry elapses.
    /// Defaults to 1/3 of the expiry time.
    /// </summary>
    public MongoDistributedSynchronizationOptionsBuilder ExtensionCadence(TimeSpan extensionCadence)
    {
        _extensionCadence = new TimeoutValue(extensionCadence, nameof(extensionCadence));
        return this;
    }

    /// <summary>
    /// Waiting to acquire a lock requires a busy wait that alternates acquire attempts and sleeps.
    /// This determines how much time is spent sleeping between attempts. Lower values will raise the
    /// volume of acquire requests under contention but will also raise the responsiveness (how long
    /// it takes a waiter to notice that a contended the lock has become available).
    /// Specifying a range of values allows the implementation to select an actual value in the range
    /// at random for each sleep. This helps avoid the case where two clients become "synchronized"
    /// in such a way that results in one client monopolizing the lock.
    /// The default is [10ms, 800ms]
    /// </summary>
    public MongoDistributedSynchronizationOptionsBuilder BusyWaitSleepTime(TimeSpan min, TimeSpan max)
    {
        TimeoutValue minTimeoutValue = new(min, nameof(min)),
            maxTimeoutValue = new(max, nameof(max));
        if (minTimeoutValue.IsInfinite) { throw new ArgumentOutOfRangeException(nameof(min), "may not be infinite"); }
        if (maxTimeoutValue.IsInfinite || maxTimeoutValue.CompareTo(min) < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(max), max, "must be non-infinite and greater than " + nameof(min));
        }
        _minBusyWaitSleepTime = minTimeoutValue;
        _maxBusyWaitSleepTime = maxTimeoutValue;
        return this;
    }

    internal static MongoDistributedLockOptions GetOptions(Action<MongoDistributedSynchronizationOptionsBuilder>? optionsBuilder)
    {
        MongoDistributedSynchronizationOptionsBuilder? options;
        if (optionsBuilder != null)
        {
            options = new();
            optionsBuilder(options);
        }
        else
        {
            options = null;
        }
        var expiry = options?._expiry ?? DefaultExpiry;
        TimeoutValue extensionCadence;
        if (options?._extensionCadence is { } specifiedExtensionCadence)
        {
            if (specifiedExtensionCadence.CompareTo(expiry) >= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(extensionCadence),
                    specifiedExtensionCadence.TimeSpan,
                    $"{nameof(extensionCadence)} must be less than {nameof(expiry)} ({expiry.TimeSpan})");
            }
            extensionCadence = specifiedExtensionCadence;
        }
        else
        {
            extensionCadence = TimeSpan.FromMilliseconds(expiry.InMilliseconds / 3.0);
        }
        return new(
            expiry: expiry,
            extensionCadence: extensionCadence,
            minBusyWaitSleepTime: options?._minBusyWaitSleepTime ?? TimeSpan.FromMilliseconds(10),
            maxBusyWaitSleepTime: options?._maxBusyWaitSleepTime ?? TimeSpan.FromSeconds(0.8));
    }
}

internal readonly struct MongoDistributedLockOptions(
    TimeoutValue expiry,
    TimeoutValue extensionCadence,
    TimeoutValue minBusyWaitSleepTime,
    TimeoutValue maxBusyWaitSleepTime)
{
    public TimeoutValue Expiry => expiry;
    public TimeoutValue ExtensionCadence => extensionCadence;
    public TimeoutValue MinBusyWaitSleepTime => minBusyWaitSleepTime;
    public TimeoutValue MaxBusyWaitSleepTime => maxBusyWaitSleepTime;
}