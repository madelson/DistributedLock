using Medallion.Threading.Internal;

namespace Medallion.Threading.Azure;

/// <summary>
/// Specifies options for an Azure table-based lock
/// </summary>
public sealed class AzureTableOptionsBuilder
{
    internal static readonly TimeoutValue DefaultLeaseDuration = TimeSpan.FromSeconds(30);

    private TimeoutValue? _duration, _renewalCadence, _minBusyWaitSleepTime, _maxBusyWaitSleepTime;
    private AzureTableKeySelector? _partitionKey, _rowKey;
    private AzureTableAcquireStrategySelector? _acquireStrategy;
    private bool? _createTableIfNotExists;

    internal AzureTableOptionsBuilder() { }

    /// <summary>
    /// Specifies how long the lease will last, absent auto-renewal.
    ///
    /// Unlike Azure blob leases, this duration is enforced by this library rather than by Azure, so there is
    /// no 15-60s restriction. Note that because expiration is evaluated against the acquiring client's clock,
    /// clock skew between clients directly affects correctness; prefer longer durations when skew is possible.
    ///
    /// Specifying <see cref="Timeout.InfiniteTimeSpan"/> creates a lease that never expires, which will
    /// require manual cleanup should the holding process terminate without releasing.
    ///
    /// Defaults to 30s.
    /// </summary>
    public AzureTableOptionsBuilder Duration(TimeSpan duration)
    {
        var durationTimeoutValue = new TimeoutValue(duration, nameof(duration));
        if (durationTimeoutValue.IsZero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "may not be zero");
        }

        this._duration = durationTimeoutValue;
        return this;
    }

    /// <summary>
    /// Determines how frequently the lease will be renewed when held. More frequent renewal means more
    /// unnecessary requests but also a lower chance of losing the lease due to the process hanging or
    /// otherwise failing to get its renewal request in before the lease duration expires.
    ///
    /// To disable auto-renewal, specify <see cref="Timeout.InfiniteTimeSpan"/>.
    ///
    /// Defaults to 1/3 of the specified lease duration (may be infinite).
    /// </summary>
    public AzureTableOptionsBuilder RenewalCadence(TimeSpan renewalCadence)
    {
        this._renewalCadence = new TimeoutValue(renewalCadence, nameof(renewalCadence));
        return this;
    }

    /// <summary>
    /// Waiting to acquire a lease requires a busy wait that alternates acquire attempts and sleeps.
    /// This determines how much time is spent sleeping between attempts.
    ///
    /// The default is [250ms, 1s].
    /// </summary>
    public AzureTableOptionsBuilder BusyWaitSleepTime(TimeSpan min, TimeSpan max)
    {
        var minTimeoutValue = new TimeoutValue(min, nameof(min));
        var maxTimeoutValue = new TimeoutValue(max, nameof(max));

        if (minTimeoutValue.IsInfinite) { throw new ArgumentOutOfRangeException(nameof(min), "may not be infinite"); }
        if (maxTimeoutValue.IsInfinite || maxTimeoutValue.CompareTo(min) < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(max), max, "must be non-infinite and greater than " + nameof(min));
        }

        this._minBusyWaitSleepTime = minTimeoutValue;
        this._maxBusyWaitSleepTime = maxTimeoutValue;
        return this;
    }

    /// <summary>
    /// Determines the entity partition key. Defaults to the constant "__DistributedLock".
    ///
    /// A constant partition key keeps all locks in one partition, which is required for any future
    /// multi-row primitives (e.g. semaphores) to use entity group transactions.
    /// </summary>
    public AzureTableOptionsBuilder PartitionKey(AzureTableKeySelector partitionKey)
    {
        this._partitionKey = partitionKey ?? throw new ArgumentNullException(nameof(partitionKey));
        return this;
    }

    /// <summary>
    /// Determines the entity row key. Defaults to the lock name, sanitized to remove characters
    /// disallowed in key fields.
    ///
    /// Note that a constant row key would cause all locks to collide on a single entity and thereby
    /// mutually exclude one another; use <see cref="AzureTableKeySelector.NamePlaceholder"/> in a
    /// template to incorporate the lock name.
    /// </summary>
    public AzureTableOptionsBuilder RowKey(AzureTableKeySelector rowKey)
    {
        this._rowKey = rowKey ?? throw new ArgumentNullException(nameof(rowKey));
        return this;
    }

    /// <summary>
    /// If true, an acquire attempt that fails because the table does not exist will create the table
    /// and retry once. Defaults to false, since creating tables is a control-plane side effect that
    /// may not be permitted by the credentials in use.
    /// </summary>
    public AzureTableOptionsBuilder CreateTableIfNotExists(bool createTableIfNotExists = true)
    {
        this._createTableIfNotExists = createTableIfNotExists;
        return this;
    }

    /// <summary>
    /// Determines how each acquire attempt probes the backing entity. May be specified as a constant
    /// <see cref="AzureTableAcquireStrategy"/> or as a function of <see cref="AzureTableAcquireContext"/>
    /// which is evaluated before every attempt.
    ///
    /// Defaults to <see cref="AzureTableAcquireStrategySelector.Adaptive"/>, which is appropriate for
    /// nearly all workloads.
    /// </summary>
    public AzureTableOptionsBuilder AcquireStrategy(AzureTableAcquireStrategySelector acquireStrategy)
    {
        this._acquireStrategy = acquireStrategy ?? throw new ArgumentNullException(nameof(acquireStrategy));
        return this;
    }

    internal static AzureTableOptions GetOptions(Action<AzureTableOptionsBuilder>? optionsBuilder)
    {
        AzureTableOptionsBuilder? options;
        if (optionsBuilder != null)
        {
            options = new AzureTableOptionsBuilder();
            optionsBuilder(options);

            if (options._renewalCadence is { } renewalCadence && !renewalCadence.IsInfinite)
            {
                var duration = options._duration ?? DefaultLeaseDuration;
                if (renewalCadence.CompareTo(duration) >= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(renewalCadence),
                        renewalCadence.TimeSpan,
                        $"{nameof(renewalCadence)} must not be larger than {nameof(duration)} ({duration}). To disable auto-renewal, specify {nameof(Timeout)}.{nameof(Timeout.InfiniteTimeSpan)}"
                    );
                }
            }
        }
        else
        {
            options = null;
        }

        var durationToUse = options?._duration ?? DefaultLeaseDuration;
        return new AzureTableOptions(
            duration: durationToUse,
            renewalCadence: options?._renewalCadence ?? (durationToUse.IsInfinite ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(durationToUse.InMilliseconds / 3.0)),
            minBusyWaitSleepTime: options?._minBusyWaitSleepTime ?? TimeSpan.FromMilliseconds(250),
            maxBusyWaitSleepTime: options?._maxBusyWaitSleepTime ?? TimeSpan.FromSeconds(1),
            partitionKey: options?._partitionKey,
            rowKey: options?._rowKey,
            acquireStrategy: options?._acquireStrategy ?? AzureTableAcquireStrategySelector.Adaptive,
            createTableIfNotExists: options?._createTableIfNotExists ?? false
        );
    }
}

/// <summary>
/// Resolved options. A named type rather than a tuple because the arity has outgrown readability.
/// Null <see cref="PartitionKey"/>/<see cref="RowKey"/> mean "use the default derivation".
/// </summary>
internal sealed class AzureTableOptions(
    TimeoutValue duration,
    TimeoutValue renewalCadence,
    TimeoutValue minBusyWaitSleepTime,
    TimeoutValue maxBusyWaitSleepTime,
    AzureTableKeySelector? partitionKey,
    AzureTableKeySelector? rowKey,
    AzureTableAcquireStrategySelector acquireStrategy,
    bool createTableIfNotExists)
{
    public TimeoutValue Duration { get; } = duration;
    public TimeoutValue RenewalCadence { get; } = renewalCadence;
    public TimeoutValue MinBusyWaitSleepTime { get; } = minBusyWaitSleepTime;
    public TimeoutValue MaxBusyWaitSleepTime { get; } = maxBusyWaitSleepTime;
    public AzureTableKeySelector? PartitionKey { get; } = partitionKey;
    public AzureTableKeySelector? RowKey { get; } = rowKey;
    public AzureTableAcquireStrategySelector AcquireStrategy { get; } = acquireStrategy;
    public bool CreateTableIfNotExists { get; } = createTableIfNotExists;
}