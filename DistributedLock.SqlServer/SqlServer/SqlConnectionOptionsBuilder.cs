using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Medallion.Threading.SqlServer
{
    public sealed class SqlConnectionOptionsBuilder
    {
        private TimeoutValue? _keepaliveCadence;
        private bool? _useTransaction, _useMultiplexing;

        /// <summary>
        /// Sets the cadence at which we run a "keepalive" query on a connection that is holding a lock.
        /// For example, on Azure idle connections are auto-killed by the connection governor.
        /// 
        /// To disable keepalive, set to <see cref="Timeout.InfiniteTimeSpan"/>.
        /// 
        /// Defaults to 10 minutes based on Azure's 30 minute default behavior.
        /// </summary>
        public SqlConnectionOptionsBuilder KeepaliveCadence(TimeSpan cadence)
        {
            this._keepaliveCadence = new TimeoutValue(cadence, nameof(cadence));
            return this;
        }

        /// <summary>
        /// Whether the lock should be taken using a transaction scope rather than a session scope. Defaults to false
        /// </summary>
        public SqlConnectionOptionsBuilder UseTransaction(bool useTransaction = true)
        {
            this._useTransaction = useTransaction;
            return this;
        }

        /// <summary>
        /// Whether the lock should attempt to share connections across locking attempts rather than give each lock
        /// a dedicated connection. This helps avoid connection pool exhaustion. Defaults to true.
        /// </summary>
        public SqlConnectionOptionsBuilder UseMultiplexing(bool useMultiplexing = true)
        {
            this._useMultiplexing = useMultiplexing;
            return this;
        }

        internal void Deconstruct(out TimeoutValue keepaliveCadence, out bool useTransaction, out bool useMultiplexing)
        {
            keepaliveCadence = this._keepaliveCadence ?? TimeSpan.FromMinutes(10);
            useTransaction = this._useTransaction ?? false;
            useMultiplexing = this._useMultiplexing ?? !useTransaction;

            if(useMultiplexing && useTransaction)
            {
                throw new ArgumentException(nameof(useTransaction) + ": is not compatible with " + nameof(useMultiplexing));
            }
        }
    }
}
