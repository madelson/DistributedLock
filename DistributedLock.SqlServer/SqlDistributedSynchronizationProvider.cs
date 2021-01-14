using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.SqlServer
{
    /// <summary>
    /// Implements <see cref="IDistributedLockProvider"/> for <see cref="SqlDistributedLock"/>,
    /// <see cref="IDistributedUpgradeableReaderWriterLockProvider"/> for <see cref="SqlDistributedReaderWriterLock"/>,
    /// and <see cref="IDistributedSemaphoreProvider"/> for <see cref="SqlDistributedSemaphore"/>.
    /// </summary>
    public sealed class SqlDistributedSynchronizationProvider : IDistributedLockProvider, IDistributedUpgradeableReaderWriterLockProvider, IDistributedSemaphoreProvider
    {
        private readonly Func<string, bool, SqlDistributedLock> _lockFactory;
        private readonly Func<string, bool, SqlDistributedReaderWriterLock> _readerWriterLockFactory;
        private readonly Func<string, int, SqlDistributedSemaphore> _semaphoreFactory;

        /// <summary>
        /// Constructs a provider that connects with <paramref name="connectionString"/> and <paramref name="options"/>.
        /// </summary>
        public SqlDistributedSynchronizationProvider(string connectionString, Action<SqlConnectionOptionsBuilder>? options = null)
        {
            if (connectionString == null) { throw new ArgumentNullException(nameof(connectionString)); }

            this._lockFactory = (name, exactName) => new SqlDistributedLock(name, connectionString, options, exactName);
            this._readerWriterLockFactory = (name, exactName) => new SqlDistributedReaderWriterLock(name, connectionString, options, exactName);
            this._semaphoreFactory = (name, maxCount) => new SqlDistributedSemaphore(name, maxCount, connectionString, options);
        }

        /// <summary>
        /// Constructs a provider that connects with <paramref name="connection"/>.
        /// </summary>
        public SqlDistributedSynchronizationProvider(IDbConnection connection)
        {
            if (connection == null) { throw new ArgumentNullException(nameof(connection)); }

            this._lockFactory = (name, exactName) => new SqlDistributedLock(name, connection, exactName);
            this._readerWriterLockFactory = (name, exactName) => new SqlDistributedReaderWriterLock(name, connection, exactName);
            this._semaphoreFactory = (name, maxCount) => new SqlDistributedSemaphore(name, maxCount, connection);
        }

        /// <summary>
        /// Constructs a provider that connects with <paramref name="transaction"/>.
        /// </summary>
        public SqlDistributedSynchronizationProvider(IDbTransaction transaction)
        {
            if (transaction == null) { throw new ArgumentNullException(nameof(transaction)); }

            this._lockFactory = (name, exactName) => new SqlDistributedLock(name, transaction, exactName);
            this._readerWriterLockFactory = (name, exactName) => new SqlDistributedReaderWriterLock(name, transaction, exactName);
            this._semaphoreFactory = (name, maxCount) => new SqlDistributedSemaphore(name, maxCount, transaction);
        }

        /// <summary>
        /// Constructs an instance of <see cref="SqlDistributedLock"/> with the provided <paramref name="name"/>. Unless <paramref name="exactName"/> 
        /// is specified, invalid applock names will be escaped/hashed.
        /// </summary>
        public SqlDistributedLock CreateLock(string name, bool exactName = false) => this._lockFactory(name, exactName);

        IDistributedLock IDistributedLockProvider.CreateLock(string name) => this.CreateLock(name);

        /// <summary>
        /// Constructs an instance of <see cref="SqlDistributedReaderWriterLock"/> with the provided <paramref name="name"/>. Unless <paramref name="exactName"/> 
        /// is specified, invalid applock names will be escaped/hashed.
        /// </summary>
        public SqlDistributedReaderWriterLock CreateReaderWriterLock(string name, bool exactName = false) => this._readerWriterLockFactory(name, exactName);

        IDistributedUpgradeableReaderWriterLock IDistributedUpgradeableReaderWriterLockProvider.CreateUpgradeableReaderWriterLock(string name) =>
            this.CreateReaderWriterLock(name);

        IDistributedReaderWriterLock IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string name) =>
            this.CreateReaderWriterLock(name);

        /// <summary>
        /// Constructs an instance of <see cref="SqlDistributedSemaphore"/> with the provided <paramref name="name"/> and <paramref name="maxCount"/>.
        /// </summary>
        public SqlDistributedSemaphore CreateSemaphore(string name, int maxCount) => this._semaphoreFactory(name, maxCount);

        IDistributedSemaphore IDistributedSemaphoreProvider.CreateSemaphore(string name, int maxCount) => this.CreateSemaphore(name, maxCount);
    }
}
