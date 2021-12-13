using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Medallion.Threading.Oracle
{
    /// <summary>
    /// Implements <see cref="IDistributedLockProvider"/> for <see cref="OracleDistributedLock"/>
    /// </summary>
    public class OracleDistributedSynchronizationProvider : IDistributedLockProvider
    {
        private readonly Func<string, bool, OracleDistributedLock> _lockFactory;

        /// <summary>
        /// Constructs a provider that connects with <paramref name="connectionString"/> and <paramref name="options"/>.
        /// </summary>
        public OracleDistributedSynchronizationProvider(string connectionString, Action<OracleConnectionOptionsBuilder>? options = null)
        {
            if (connectionString == null) { throw new ArgumentNullException(nameof(connectionString)); }

            this._lockFactory = (name, exactName) => new OracleDistributedLock(name, connectionString, options, exactName);
        }

        /// <summary>
        /// Constructs a provider that connects with <paramref name="connection"/>.
        /// </summary>
        public OracleDistributedSynchronizationProvider(IDbConnection connection)
        {
            if (connection == null) { throw new ArgumentNullException(nameof(connection)); }

            this._lockFactory = (name, exactName) => new OracleDistributedLock(name, connection, exactName);
        }

        /// <summary>
        /// Creates a <see cref="OracleDistributedLock"/> with the provided <paramref name="name"/>. Unless <paramref name="exactName"/> 
        /// is specified, invalid names will be escaped/hashed.
        /// </summary>
        public OracleDistributedLock CreateLock(string name, bool exactName = false) => this._lockFactory(name, exactName);

        IDistributedLock IDistributedLockProvider.CreateLock(string name) => this.CreateLock(name);
    }
}
