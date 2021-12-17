using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Oracle
{
    /// <summary>
    /// Implements an upgradeable distributed reader-writer lock for the Oracle database using the DBMS_LOCK package.
    /// </summary>
    public sealed partial class OracleDistributedReaderWriterLock : IInternalDistributedUpgradeableReaderWriterLock<OracleDistributedReaderWriterLockHandle, OracleDistributedReaderWriterLockUpgradeableHandle>
    {
        private readonly IDbDistributedLock _internalLock;

        /// <summary>
        /// Constructs a new lock using the provided <paramref name="name"/>. 
        /// 
        /// The provided <paramref name="connectionString"/> will be used to connect to the database.
        /// 
        /// Unless <paramref name="exactName"/> is specified, <paramref name="name"/> will be escaped/hashed to ensure name validity.
        /// </summary>
        public OracleDistributedReaderWriterLock(string name, string connectionString, Action<OracleConnectionOptionsBuilder>? options = null, bool exactName = false)
            : this(name, exactName, n => OracleDistributedLock.CreateInternalLock(n, connectionString, options))
        {
        }

        /// <summary>
        /// Constructs a new lock using the provided <paramref name="name"/>.
        /// 
        /// The provided <paramref name="connection"/> will be used to connect to the database and will provide lock scope. It is assumed to be externally managed and
        /// will not be opened or closed.
        /// 
        /// Unless <paramref name="exactName"/> is specified, <paramref name="name"/> will be escaped/hashed to ensure name validity.
        /// </summary>
        public OracleDistributedReaderWriterLock(string name, IDbConnection connection, bool exactName = false)
            : this(name, exactName, n => OracleDistributedLock.CreateInternalLock(n, connection))
        {
        }

        private OracleDistributedReaderWriterLock(string name, bool exactName, Func<string, IDbDistributedLock> internalLockFactory)
        {
            this.Name = OracleDistributedLock.GetName(name, exactName);
            this._internalLock = internalLockFactory(this.Name);
        }

        /// <summary>
        /// Implements <see cref="IDistributedLock.Name"/>
        /// </summary>
        public string Name { get; }

        async ValueTask<OracleDistributedReaderWriterLockUpgradeableHandle?> IInternalDistributedUpgradeableReaderWriterLock<OracleDistributedReaderWriterLockHandle, OracleDistributedReaderWriterLockUpgradeableHandle>.InternalTryAcquireUpgradeableReadLockAsync(
            TimeoutValue timeout,
            CancellationToken cancellationToken)
        {
            var innerHandle = await this._internalLock
                .TryAcquireAsync(timeout, OracleDbmsLock.UpdateLock, cancellationToken, contextHandle: null).ConfigureAwait(false);
            return innerHandle != null ? new OracleDistributedReaderWriterLockUpgradeableHandle(innerHandle, this._internalLock) : null;
        }

        async ValueTask<OracleDistributedReaderWriterLockHandle?> IInternalDistributedReaderWriterLock<OracleDistributedReaderWriterLockHandle>.InternalTryAcquireAsync(
            TimeoutValue timeout,
            CancellationToken cancellationToken,
            bool isWrite)
        {
            var innerHandle = await this._internalLock
                .TryAcquireAsync(timeout, isWrite ? OracleDbmsLock.ExclusiveLock : OracleDbmsLock.SharedLock, cancellationToken, contextHandle: null).ConfigureAwait(false);
            return innerHandle != null ? new OracleDistributedReaderWriterLockNonUpgradeableHandle(innerHandle) : null;
        }
    }
}
