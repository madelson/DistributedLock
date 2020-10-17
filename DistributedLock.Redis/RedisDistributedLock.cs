using Medallion.Threading.Internal;
using StackExchange.Redis;
using StackExchange.Redis.KeyspaceIsolation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Redis
{
    public sealed partial class RedisDistributedLock : IInternalDistributedLock<RedisDistributedLockHandle>
    {
        private static readonly LuaScript ReleaseScript = LuaScript.Prepare(@"
            if redis.call(""get"", @key) == @lockId then
                return redis.call(""del"", @key)
            else
                return 0
            end"
        );

        private static readonly LuaScript ExtendScript = LuaScript.Prepare(@"
            if redis.call(""get"", @key) == @lockId then
                return redis.call(""pexpire"", @key, @ttl)
            else
                return 0
            end"
        );

        private static readonly string LockIdPrefix;

        private readonly IReadOnlyList<IDatabase> _databases;
        private readonly (TimeoutValue expiry, TimeoutValue extensionCadence, TimeoutValue minValidityTime, TimeSpan minBusyWaitSleepTime, TimeSpan maxBusyWaitSleepTime) _options;

        static RedisDistributedLock()
        {
            using var currentProcess = Process.GetCurrentProcess();
            LockIdPrefix = $"{Environment.MachineName}_{currentProcess.Id}_";
        }
        
        public RedisDistributedLock(RedisKey key, IDatabase database, Action<RedisDistributedLockOptionsBuilder>? options = null)
            : this(key, new[] { database ?? throw new ArgumentNullException(nameof(database)) }, options)
        {
        }

        public RedisDistributedLock(RedisKey key, IEnumerable<IDatabase> databases, Action<RedisDistributedLockOptionsBuilder>? options = null)
        {
            if (key == default(RedisKey)) { throw new ArgumentNullException(nameof(key)); }
            this._databases = databases?.ToArray() ?? throw new ArgumentNullException(nameof(databases));
            if (this._databases.Count == 0) { throw new ArgumentException("may not be empty", nameof(databases)); }
            if (this._databases.Contains(null!)) { throw new ArgumentNullException(nameof(databases), "may not contain null"); }

            this.Key = key;
            this._options = RedisDistributedLockOptionsBuilder.GetOptions(options);
        }

        public RedisKey Key { get; }
        string IDistributedLock.Name => this.Key.ToString();

        ValueTask<RedisDistributedLockHandle?> IInternalDistributedLock<RedisDistributedLockHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken) =>
            BusyWaitHelper.WaitAsync(
                state: this,
                tryGetValue: (@this, cancellationToken) => @this.TryAcquireAsync(cancellationToken),
                timeout: timeout,
                minSleepTime: this._options.minBusyWaitSleepTime,
                maxSleepTime: this._options.maxBusyWaitSleepTime,
                cancellationToken: cancellationToken
            );

        private async ValueTask<RedisDistributedLockHandle?> TryAcquireAsync(CancellationToken cancellationToken)
        {
            var primitive = new RedisLockPrimitive(this);
            var tryAcquireTasks = await new RedLockAcquire(primitive, this._databases, cancellationToken).TryAcquireAsync().ConfigureAwait(false);
            return tryAcquireTasks != null ? new RedisDistributedLockHandle(primitive, tryAcquireTasks) : null;
        }

        private sealed class RedisLockPrimitive : IRedisSynchronizationPrimitive
        {
            private readonly RedisValue _lockId = LockIdPrefix + Guid.NewGuid().ToString("n");
            private readonly RedisDistributedLock _lock;

            private object? _cachedReleaseParameters, _cachedExtendParameters;

            public RedisLockPrimitive(RedisDistributedLock @lock)
            {
                this._lock = @lock;
            }

            private object ReleaseParameters => this._cachedReleaseParameters ??= new { key = this._lock.Key, lockId = this._lockId };
            private object ExtendParameters => this._cachedExtendParameters ??= new { key = this._lock.Key, lockId = this._lockId, ttl = this._lock._options.expiry.InMilliseconds };

            TimeoutValue IRedisSynchronizationPrimitive.AcquireTimeout => this._lock._options.expiry.TimeSpan - this._lock._options.minValidityTime.TimeSpan;

            TimeoutValue IRedisSynchronizationPrimitive.ExtensionCadence => this._lock._options.extensionCadence;

            TimeoutValue IRedisSynchronizationPrimitive.Expiry => this._lock._options.expiry;

            void IRedisSynchronizationPrimitive.Release(IDatabase database, bool fireAndForget) =>
                ReleaseScript.Evaluate(database, this.ReleaseParameters, flags: this.ReleaseCommandFlags(fireAndForget));

            Task IRedisSynchronizationPrimitive.ReleaseAsync(IDatabaseAsync database, bool fireAndForget) =>
                ReleaseScript.EvaluateAsync(database, this.ReleaseParameters, flags: this.ReleaseCommandFlags(fireAndForget));

            bool IRedisSynchronizationPrimitive.TryAcquire(IDatabase database) =>
                database.StringSet(this._lock.Key, this._lockId, this._lock._options.expiry.TimeSpan, When.NotExists, CommandFlags.DemandMaster);

            Task<bool> IRedisSynchronizationPrimitive.TryAcquireAsync(IDatabaseAsync database) =>
                database.StringSetAsync(this._lock.Key, this._lockId, this._lock._options.expiry.TimeSpan, When.NotExists, CommandFlags.DemandMaster);

            async Task<bool> IRedisSynchronizationPrimitive.TryExtendAsync(IDatabaseAsync database) =>
                (bool)await ExtendScript.EvaluateAsync(database, this.ExtendParameters, flags: CommandFlags.DemandMaster).ConfigureAwait(false);

            private CommandFlags ReleaseCommandFlags(bool fireAndForget) => CommandFlags.DemandMaster | (fireAndForget ? CommandFlags.FireAndForget : CommandFlags.None);
        }
    }
}
