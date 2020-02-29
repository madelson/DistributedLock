using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal
{
    /// <summary>
    /// A type which can only store a valid timeout value
    /// </summary>
#if DEBUG
    public
#else
    internal
#endif    
    readonly struct TimeoutValue : IEquatable<TimeoutValue>
    {
        public TimeoutValue(TimeSpan? timeout)
        {
            if (timeout is { } timeoutValue)
            {
                // based on Task.Wait(TimeSpan) 
                // https://referencesource.microsoft.com/#mscorlib/system/threading/Tasks/Task.cs,855657030ba22f78

                var totalMilliseconds = (long)timeoutValue.TotalMilliseconds;
                if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(
                        paramName: nameof(timeout), 
                        actualValue: timeoutValue, 
                        message: $"Must be {nameof(Timeout)}.{nameof(Timeout.InfiniteTimeSpan)} ({Timeout.InfiniteTimeSpan}) or a non-negative value <= {TimeSpan.FromMilliseconds(int.MaxValue)})"
                    );
                }

                this.InMilliseconds = (int)totalMilliseconds;
            }
            else
            {
                this.InMilliseconds = Timeout.Infinite;
            }
        }

        public int InMilliseconds { get; }
        public int InSeconds => this.IsInfinite ? throw new InvalidOperationException("infinite timeout cannot be converted to seconds") : this.InMilliseconds / 1000;
        public bool IsInfinite => this.InMilliseconds == Timeout.Infinite;
        public bool IsZero => this.InMilliseconds == 0;

        public bool Equals(TimeoutValue that) => this.InMilliseconds == that.InMilliseconds;
        public override bool Equals(object? obj) => obj is TimeoutValue that && this.Equals(that);
        public override int GetHashCode() => this.InMilliseconds;

        public static bool operator ==(TimeoutValue a, TimeoutValue b) => a.Equals(b);
        public static bool operator !=(TimeoutValue a, TimeoutValue b) => !(a == b);

        public static implicit operator TimeoutValue(TimeSpan? timeout) => new TimeoutValue(timeout);


        public override string ToString() => 
            this.IsInfinite ? "∞" 
                : this.IsZero ? "0" 
                : TimeSpan.FromMilliseconds(this.InMilliseconds).ToString();
    }
}
