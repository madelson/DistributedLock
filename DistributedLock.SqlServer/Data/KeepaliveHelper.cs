using Medallion.Threading.Internal;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Data
{
    // todo get rid of this class
    internal sealed class KeepaliveHelper
    {
        // 10-minutes is based on http://searchsqlserver.techtarget.com/feature/Why-you-should-think-twice-about-Windows-Azure-SQL-Database
        // which says Azure closes connections after being idle for 30 minutes
        private static long _intervalTicks = TimeSpan.FromMinutes(10).Ticks;

        public static TimeSpan Interval
        {
            get { return TimeSpan.FromTicks(Volatile.Read(ref _intervalTicks)); }
            // for testing
            set { Volatile.Write(ref _intervalTicks, value.Ticks); }
        }
    }
}
