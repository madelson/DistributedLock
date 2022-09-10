using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading
{
    /// <summary>
    /// Tracks how much of a timeout value has elapsed.
    /// </summary>
    internal readonly struct TimeoutTracker
    {
        private readonly TimeoutValue _timeout;
        private readonly int _initialTickCountMillis;

        public TimeoutTracker(TimeoutValue timeout)
        {
            this._timeout = timeout;
            this._initialTickCountMillis = NeedsTracking(timeout) ? Environment.TickCount : 0;
        }

        public TimeoutValue Remaining =>
            NeedsTracking(this._timeout)
                ? new(TimeSpan.FromMilliseconds(Math.Max(this._timeout.InMilliseconds - (Environment.TickCount - this._initialTickCountMillis), 0)))
                : this._timeout;

        private static bool NeedsTracking(TimeoutValue timeout) => !(timeout.IsInfinite || timeout.IsZero);
    }
}
