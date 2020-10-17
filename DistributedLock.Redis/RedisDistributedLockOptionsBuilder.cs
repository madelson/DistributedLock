using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Redis
{
    // todo consider rename to RedLockOptionsBuilder
    /// <summary>
    /// Options for configuring a redis-based lock
    /// </summary>
    public sealed class RedisDistributedLockOptionsBuilder
    {
        internal static readonly TimeoutValue DefaultExpiry = TimeSpan.FromSeconds(30);
        /// <summary>
        /// We don't want to allow expiry to go too low, since then the lock doesn't even work (and the default
        /// min observed expiry will end up greater than the default expiry)
        /// </summary>
        internal static readonly TimeoutValue MinimumExpiry = TimeSpan.FromSeconds(.1);

        private TimeoutValue? _expiry, 
            _extensionCadence, 
            _minValidityTime,
            _minBusyWaitSleepTime, 
            _maxBusyWaitSleepTime; 
        
        internal RedisDistributedLockOptionsBuilder() { }

        /// <summary>
        /// Specifies how long the lock will last, absent auto-extension. Because auto-extension exists,
        /// this value generally will have little effect on program behavior. However, making the expiry longer means that
        /// auto-extension requests can occur less frequently, saving resources. On the other hand, when a lock is abandoned
        /// without explicit release (e. g. if the holding process crashes), the expiry determines how long other processes
        /// would need to wait in order to acquire it.
        /// 
        /// Defaults to 30s.
        /// </summary>
        public RedisDistributedLockOptionsBuilder Expiry(TimeSpan expiry)
        {
            var expiryTimeoutValue = new TimeoutValue(expiry, nameof(expiry));
            if (expiryTimeoutValue.IsInfinite || expiryTimeoutValue.CompareTo(MinimumExpiry) < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(expiry), expiry, $"Must be >= {MinimumExpiry.TimeSpan} and < ∞");
            }
            this._expiry = expiryTimeoutValue;
            return this;
        }

        /// <summary>
        /// Determines how frequently the lock will be extended while held. More frequent extension means more unnecessary requests
        /// but also a lower chance of losing the lock due to the process hanging or otherwise failing to get its extension request in
        /// before the lock expiry elapses.
        /// 
        /// Defaults to 1/3 of the specified <see cref="MinValidityTime(TimeSpan)"/>.
        /// </summary>
        public RedisDistributedLockOptionsBuilder ExtensionCadence(TimeSpan extensionCadence)
        {
            this._extensionCadence = new TimeoutValue(extensionCadence, nameof(extensionCadence));
            return this;
        }

        /// <summary>
        /// The lock expiry determines how long the lock will be held without being extended. However, since it takes some amount
        /// of time to acquire the lock, we will not have all of expiry available upon acquisition.
        /// 
        /// This value sets a minimum amount which we'll be guaranteed to have left once acquisition completes.
        /// 
        /// Defaults to 90% of the specified lock expiry.
        /// </summary>
        public RedisDistributedLockOptionsBuilder MinValidityTime(TimeSpan minValidityTime)
        {
            var minValidityTimeoutValue = new TimeoutValue(minValidityTime, nameof(minValidityTime));
            if (minValidityTimeoutValue.IsZero)
            {
                throw new ArgumentOutOfRangeException(nameof(minValidityTime), minValidityTime, "may not be zero");
            }
            this._minValidityTime = minValidityTimeoutValue;
            return this;
        }

        /// <summary>
        /// Waiting to acquire a lock requires a busy wait that alternates acquire attempts and sleeps.
        /// This determines how much time is spent sleeping between attempts. Lower values will raise the
        /// volume of acquire requests under contention but will also raise the responsiveness (how long
        /// it takes a waiter to notice that a contended the lock has become available).
        /// 
        /// Specifying a range of values allows the implementation to select an actual value in the range 
        /// at random for each sleep. This helps avoid the case where two clients become "synchronized"
        /// in such a way that results in one client monopolizing the lock.
        /// 
        /// The default is [10ms, 800ms]
        /// </summary>
        public RedisDistributedLockOptionsBuilder BusyWaitSleepTime(TimeSpan min, TimeSpan max)
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

        internal static (TimeoutValue expiry, TimeoutValue extensionCadence, TimeoutValue minValidityTime, TimeSpan minBusyWaitSleepTime, TimeSpan maxBusyWaitSleepTime) GetOptions(Action<RedisDistributedLockOptionsBuilder>? optionsBuilder)
        {
            RedisDistributedLockOptionsBuilder? options;
            if (optionsBuilder != null)
            {
                options = new RedisDistributedLockOptionsBuilder();
                optionsBuilder(options);
            }
            else
            {
                options = null;
            }

            var expiry = options?._expiry ?? DefaultExpiry;

            TimeoutValue minValidityTime;
            if (options?._minValidityTime is { } specifiedMinValidityTime)
            {
                if (specifiedMinValidityTime.CompareTo(expiry) >= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(minValidityTime),
                        specifiedMinValidityTime.TimeSpan,
                        $"{nameof(minValidityTime)} must be less than {nameof(expiry)} ({expiry.TimeSpan})"
                    );
                }
                minValidityTime = specifiedMinValidityTime;
            }
            else
            {
                minValidityTime = TimeSpan.FromMilliseconds(Math.Max(0.9 * expiry.InMilliseconds, 1));
            }

            TimeoutValue extensionCadence;
            if (options?._extensionCadence is { } specifiedExtensionCadence)
            {
                if (specifiedExtensionCadence.CompareTo(minValidityTime) >= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(extensionCadence),
                        specifiedExtensionCadence.TimeSpan,
                        $"{nameof(extensionCadence)} must be less than {nameof(expiry)} ({expiry.TimeSpan}). To disable auto-extension, specify {nameof(Timeout)}.{nameof(Timeout.InfiniteTimeSpan)}"
                    );
                }
                extensionCadence = specifiedExtensionCadence;
            }
            else
            {
                extensionCadence = TimeSpan.FromMilliseconds(minValidityTime.InMilliseconds / 3.0);
            }

            return (
                expiry,
                extensionCadence,
                minValidityTime,
                minBusyWaitSleepTime: options?._minBusyWaitSleepTime?.TimeSpan ?? TimeSpan.FromMilliseconds(10),
                maxBusyWaitSleepTime: options?._maxBusyWaitSleepTime?.TimeSpan ?? TimeSpan.FromSeconds(0.8)
            );
        }
    }
}
