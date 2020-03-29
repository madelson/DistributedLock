using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Medallion.Threading.Postgres
{
    public sealed class PostgresConnectionOptionsBuilder
    {
        private TimeoutValue? _keepaliveCadence;
        private bool? _useMultiplexing;

        internal PostgresConnectionOptionsBuilder() { }

        /// <summary>
        /// Some Postgres setups have automation in place which aggressively kills idle connections.
        /// 
        /// To prevent this, this option sets the cadence at which we run a no-op "keepalive" query on a connection that is holding a lock. 
        /// Note that this still does not guarantee protection for the connection from all conditions where the governor might kill it.
        /// 
        /// Defaults to <see cref="Timeout.InfiniteTimeSpan"/>, which disables keepalive.
        /// </summary>
        public PostgresConnectionOptionsBuilder KeepaliveCadence(TimeSpan keepaliveCadence)
        {
            this._keepaliveCadence = new TimeoutValue(keepaliveCadence, nameof(keepaliveCadence));
            return this;
        }

        /// <summary>
        /// This mode takes advantage of the fact that while "holding" a lock (or other synchronization primitive)
        /// a connection is essentially idle. Thus, rather than creating a new connection for each held lock it is 
        /// often possible to multiplex a shared connection so that that connection can hold multiple locks at the same time.
        /// 
        /// Multiplexing is on by default.
        /// 
        /// This is implemented in such a way that releasing a lock held on such a connection will never be blocked by an
        /// Acquire() call that is waiting to acquire a lock on that same connection. For this reason, the multiplexing
        /// strategy is "optimistic": if the lock can't be acquired instantaneously on the shared connection, a new (shareable) 
        /// connection will be allocated.
        /// 
        /// This option can improve performance and avoid connection pool starvation in high-load scenarios. It is also
        /// particularly applicable to cases where <see cref="IDistributedLock.TryAcquire(TimeSpan, System.Threading.CancellationToken)"/>
        /// semantics are used with a zero-length timeout.
        /// </summary>
        public PostgresConnectionOptionsBuilder UseMultiplexing(bool useMultiplexing = true)
        {
            this._useMultiplexing = useMultiplexing;
            return this;
        }

        internal static (TimeoutValue keepaliveCadence, bool useMultiplexing) GetOptions(Action<PostgresConnectionOptionsBuilder>? optionsBuilder)
        {
            PostgresConnectionOptionsBuilder? options;
            if (optionsBuilder != null)
            {
                options = new PostgresConnectionOptionsBuilder();
                optionsBuilder(options);
            }
            else
            {
                options = null;
            }

            var keepaliveCadence = options?._keepaliveCadence ?? Timeout.InfiniteTimeSpan;
            var useMultiplexing = options?._useMultiplexing ?? true;

            return (keepaliveCadence, useMultiplexing);
        }
    }
}
