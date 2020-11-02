using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Medallion.Threading.Azure
{
    /// <summary>
    /// Specifies options for an Azure blob lease
    /// </summary>
    public sealed class AzureBlobLeaseOptionsBuilder
    {
        /// <summary>
        /// From https://docs.microsoft.com/en-us/rest/api/storageservices/lease-blob:
        /// "The lock duration can be 15 to 60 seconds, or can be infinite"
        /// </summary>
        internal static readonly TimeoutValue MinLeaseDuration = TimeSpan.FromSeconds(15),
            MaxNonInfiniteLeaseDuration = TimeSpan.FromSeconds(60),
            DefaultLeaseDuration = TimeSpan.FromSeconds(30);

        private TimeoutValue? _duration, _renewalCadence, _minBusyWaitSleepTime, _maxBusyWaitSleepTime;

        internal AzureBlobLeaseOptionsBuilder() { }

        /// <summary>
        /// Specifies how long the lease will last, absent auto-renewal.
        /// 
        /// If auto-renewal is enabled (the default), then a shorter duration means more frequent auto-renewal requests,
        /// while an infinite duration means no auto-renewal requests. Furthermore, if the lease-holding process were to
        /// exit without explicitly releasing, then duration determines how long other processes would need to wait in 
        /// order to acquire the lease.
        /// 
        /// If auto-renewal is disabled, then duration determines how long the lease will be held.
        /// 
        /// Defaults to 30s.
        /// </summary>
        public AzureBlobLeaseOptionsBuilder Duration(TimeSpan duration)
        {
            var durationTimeoutValue = new TimeoutValue(duration, nameof(duration));
            if (durationTimeoutValue.CompareTo(MinLeaseDuration) < 0
                || (!durationTimeoutValue.IsInfinite && durationTimeoutValue.CompareTo(MaxNonInfiniteLeaseDuration) > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(duration), duration, $"Must be infinite or in [{MinLeaseDuration}, {MaxNonInfiniteLeaseDuration}]");
            }

            this._duration = durationTimeoutValue;
            return this;
        }

        /// <summary>
        /// Determines how frequently the lease will be renewed when held. More frequent renewal means more unnecessary requests
        /// but also a lower chance of losing the lease due to the process hanging or otherwise failing to get its renewal request in
        /// before the lease duration expires.
        /// 
        /// To disable auto-renewal, specify <see cref="Timeout.InfiniteTimeSpan"/>
        /// 
        /// Defaults to 1/3 of the specified lease duration (may be infinite).
        /// </summary>
        public AzureBlobLeaseOptionsBuilder RenewalCadence(TimeSpan renewalCadence)
        {
            this._renewalCadence = new TimeoutValue(renewalCadence, nameof(renewalCadence));
            return this;
        }

        /// <summary>
        /// Waiting to acquire a lease requires a busy wait that alternates acquire attempts and sleeps.
        /// This determines how much time is spent sleeping between attempts. Lower values will raise the
        /// volume of acquire requests under contention but will also raise the responsiveness (how long
        /// it takes a waiter to notice that a contended the lease has become available).
        /// 
        /// Specifying a range of values allows the implementation to select an actual value in the range 
        /// at random for each sleep. This helps avoid the case where two clients become "synchronized"
        /// in such a way that results in one client monopolizing the lease.
        /// 
        /// The default is [250ms, 1s]
        /// </summary>
        public AzureBlobLeaseOptionsBuilder BusyWaitSleepTime(TimeSpan min, TimeSpan max)
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

        internal static (TimeoutValue duration, TimeoutValue renewalCadence, TimeoutValue minBusyWaitSleepTime, TimeoutValue maxBusyWaitSleepTime) GetOptions(Action<AzureBlobLeaseOptionsBuilder>? optionsBuilder)
        {
            AzureBlobLeaseOptionsBuilder? options;
            if (optionsBuilder != null)
            {
                options = new AzureBlobLeaseOptionsBuilder();
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
            return (
                duration: durationToUse,
                renewalCadence: options?._renewalCadence ?? (durationToUse.IsInfinite ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(durationToUse.InMilliseconds / 3.0)),
                minBusyWaitSleepTime: options?._minBusyWaitSleepTime ?? TimeSpan.FromMilliseconds(250),
                maxBusyWaitSleepTime: options?._maxBusyWaitSleepTime ?? TimeSpan.FromSeconds(1)
            );
        }
    }
}
