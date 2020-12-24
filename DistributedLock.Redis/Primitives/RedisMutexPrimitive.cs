using Medallion.Threading.Internal;
using Medallion.Threading.Redis.RedLock;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Redis.Primitives
{
    internal class RedisMutexPrimitive : IRedLockAcquirableSynchronizationPrimitive, IRedLockExtensibleSynchronizationPrimitive
    {
        private readonly RedisKey _key;
        private readonly RedisValue _lockId;
        private readonly RedLockTimeouts _timeouts;

        public RedisMutexPrimitive(RedisKey key, RedisValue lockId, RedLockTimeouts timeouts)
        {
            this._key = key;
            this._lockId = lockId;
            this._timeouts = timeouts;
        }

        public TimeoutValue AcquireTimeout => this._timeouts.AcquireTimeout;

        private static readonly RedisScript<RedisMutexPrimitive> ReleaseScript = new RedisScript<RedisMutexPrimitive>(@"
            if redis.call('get', @key) == @lockId then
                return redis.call('del', @key)
            end
            return 0",
            p => new { key = p._key, lockId = p._lockId }
        );

        public void Release(IDatabase database, bool fireAndForget) => ReleaseScript.Execute(database, this, fireAndForget);
        public Task ReleaseAsync(IDatabaseAsync database, bool fireAndForget) => ReleaseScript.ExecuteAsync(database, this, fireAndForget);

        public bool TryAcquire(IDatabase database) =>
            database.StringSet(this._key, this._lockId, this._timeouts.Expiry.TimeSpan, When.NotExists, CommandFlags.DemandMaster);
        public Task<bool> TryAcquireAsync(IDatabaseAsync database) =>
            database.StringSetAsync(this._key, this._lockId, this._timeouts.Expiry.TimeSpan, When.NotExists, CommandFlags.DemandMaster);

        private static readonly RedisScript<RedisMutexPrimitive> ExtendScript = new RedisScript<RedisMutexPrimitive>(@"
            if redis.call('get', @key) == @lockId then
                return redis.call('pexpire', @key, @expiryMillis)
            end
            return 0",
            p => new { key = p._key, lockId = p._lockId, expiryMillis = p._timeouts.Expiry.InMilliseconds }
        );

        public Task<bool> TryExtendAsync(IDatabaseAsync database) => ExtendScript.ExecuteAsync(database, this).AsBooleanTask();
    }
}
