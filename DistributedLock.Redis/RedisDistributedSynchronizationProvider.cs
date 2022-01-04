using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Redis
{
    /// <summary>
    /// Implements <see cref="IDistributedLockProvider"/> for <see cref="RedisDistributedLock"/>,
    /// <see cref="IDistributedReaderWriterLockProvider"/> for <see cref="RedisDistributedReaderWriterLock"/>,
    /// and <see cref="IDistributedSemaphoreProvider"/> for <see cref="RedisDistributedSemaphore"/>.
    /// </summary>
    public sealed class RedisDistributedSynchronizationProvider : IDistributedLockProvider, IDistributedReaderWriterLockProvider, IDistributedSemaphoreProvider
    {
        private readonly IReadOnlyList<IDatabase> _databases;
        private readonly Action<RedisDistributedSynchronizationOptionsBuilder>? _options;

        /// <summary>
        /// Constructs a <see cref="RedisDistributedSynchronizationProvider"/> that connects to the provided <paramref name="database"/>
        /// and uses the provided <paramref name="options"/>.
        /// </summary>
        public RedisDistributedSynchronizationProvider(IDatabase database, Action<RedisDistributedSynchronizationOptionsBuilder>? options = null)
            : this(new[] { database ?? throw new ArgumentNullException(nameof(database)) }, options)
        {
        }

#if NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// Constructs a <see cref="RedisDistributedSynchronizationProvider"/> that connects to redis according to the provided <paramref name="connectionString"/>
        /// and the <paramref name="db"/> with the provided <paramref name="options"/>.
        /// </summary>
        /// <param name="connectionString">Redis connection string</param>
        /// <param name="db">Redis Database</param>
        /// <param name="options">Redis Distributed Synchronization Options</param>
        public RedisDistributedSynchronizationProvider(string connectionString, int db = -1, Action<RedisDistributedSynchronizationOptionsBuilder>? options = null)
            : this(ConfigurationOptions.Parse(connectionString), db, options)
        {
        }


        /// <summary>
        /// Constructs a <see cref="RedisDistributedSynchronizationProvider"/> that connects to redis according to the provided <paramref name="configurationOptions"/>
        /// and the <paramref name="db"/> with the provided <paramref name="options"/>.
        /// </summary>
        /// <param name="configurationOptions">StackExchange Redis Configuration Options</param>
        /// <param name="db">Redis Database</param>
        /// <param name="options">Redis Distributed Synchronization Options</param>
        public RedisDistributedSynchronizationProvider(ConfigurationOptions configurationOptions, int db = -1, Action<RedisDistributedSynchronizationOptionsBuilder>? options = null)
            : this(ConnectionMultiplexer.Connect(configurationOptions).GetDatabase(db), options)
        {
        }
#endif

        /// <summary>
        /// Constructs a <see cref="RedisDistributedSynchronizationProvider"/> that connects to the provided <paramref name="databases"/>
        /// and uses the provided <paramref name="options"/>.
        /// 
        /// Note that if multiple <see cref="IDatabase"/>s are provided, <see cref="CreateSemaphore(RedisKey, int)"/> will use only the first
        /// <see cref="IDatabase"/>.
        /// </summary>
        public RedisDistributedSynchronizationProvider(IEnumerable<IDatabase> databases, Action<RedisDistributedSynchronizationOptionsBuilder>? options = null)
        {
            this._databases = RedisDistributedLock.ValidateDatabases(databases);
            this._options = options;
        }

        /// <summary>
        /// Creates a <see cref="RedisDistributedLock"/> using the given <paramref name="key"/>.
        /// </summary>
        public RedisDistributedLock CreateLock(RedisKey key) => new RedisDistributedLock(key, this._databases, this._options);

        IDistributedLock IDistributedLockProvider.CreateLock(string name) => this.CreateLock(name);

        /// <summary>
        /// Creates a <see cref="RedisDistributedReaderWriterLock"/> using the given <paramref name="name"/>.
        /// </summary>
        public RedisDistributedReaderWriterLock CreateReaderWriterLock(string name) => 
            new RedisDistributedReaderWriterLock(name, this._databases, this._options);

        IDistributedReaderWriterLock IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string name) =>
            this.CreateReaderWriterLock(name);

        /// <summary>
        /// Creates a <see cref="RedisDistributedSemaphore"/> using the provided <paramref name="key"/> and <paramref name="maxCount"/>.
        /// </summary>
        public RedisDistributedSemaphore CreateSemaphore(RedisKey key, int maxCount) => new RedisDistributedSemaphore(key, maxCount, this._databases[0], this._options);

        IDistributedSemaphore IDistributedSemaphoreProvider.CreateSemaphore(string name, int maxCount) =>
            this.CreateSemaphore(name, maxCount);
    }
}
