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
    internal class RedisReadLockPrimitive : IRedLockAcquirableSynchronizationPrimitive, IRedLockExtensibleSynchronizationPrimitive
    {
        private readonly RedisValue _lockId = RedLockHelper.CreateLockId();
        private readonly RedisKey _readerKey, _writerKey;
        private readonly RedLockTimeouts _timeouts;

        public RedisReadLockPrimitive(RedisKey readerKey, RedisKey writerKey, RedLockTimeouts timeouts)
        {
            this._readerKey = readerKey;
            this._writerKey = writerKey;
            this._timeouts = timeouts;
        }

        public TimeoutValue AcquireTimeout => this._timeouts.AcquireTimeout;

        /// <summary>
        /// RELEASE READ
        /// 
        /// Just remove our ID from the reader set (noop if it wasn't there or the set DNE)
        /// </summary>
        private static readonly RedisScript<RedisReadLockPrimitive> ReleaseReadScript = new RedisScript<RedisReadLockPrimitive>(
            @"redis.call('srem', @readerKey, @lockId)",
            p => new { readerKey = p._readerKey, lockId = p._lockId }
        );

        public void Release(IDatabase database, bool fireAndForget) => ReleaseReadScript.Execute(database, this, fireAndForget);
        public Task ReleaseAsync(IDatabaseAsync database, bool fireAndForget) => ReleaseReadScript.ExecuteAsync(database, this, fireAndForget);

        /// <summary>
        /// TRY EXTEND READ
        /// 
        /// First, check if the reader set exists and our ID is still a member. If not, we fail.
        /// 
        /// Then, extend the reader set TTL to be at least our expiry (at least because other readers might be operating with a longer expiry)
        /// </summary>
        private static readonly RedisScript<RedisReadLockPrimitive> TryExtendReadScript = new RedisScript<RedisReadLockPrimitive>(@"
            if redis.call('sismember', @readerKey, @lockId) == 0 then
                return 0
            end
            if readerTtl < @expiryMillis then
                redis.call('pexpire', @readerKey, @expiryMillis)
            end
            return 1",
            p => new { readerKey = p._readerKey, lockId = p._lockId, expiryMillis = p._timeouts.Expiry.InMilliseconds }
        );

        public Task<bool> TryExtendAsync(IDatabaseAsync database) => TryExtendReadScript.ExecuteAsync(database, this).AsBooleanTask();

        /// <summary>
        /// TRY ACQUIRE READ
        /// 
        /// First, check the writer lock value: if it exists then we fail.
        /// 
        /// Then, add our ID to the reader set, creating it if it does not exist. Then, extend the TTL
        /// of the reader set to be at least our expiry. Return success.
        /// </summary>
        private static readonly RedisScript<RedisReadLockPrimitive> TryAcquireReadScript = new RedisScript<RedisReadLockPrimitive>($@"
            if redis.call('exists', @writerKey) == 1 then
                return 0
            end
            redis.call('sadd', @readerKey, @lockId)
            local readerTtl = redis.call('pttl', @readerKey)
            if readerTtl < tonumber(@expiryMillis) then
                redis.call('pexpire', @readerKey, @expiryMillis)
            end
            return 1",
            p => new { writerKey = p._writerKey, readerKey = p._readerKey, lockId = p._lockId, expiryMillis = p._timeouts.Expiry.InMilliseconds }
        );

        public Task<bool> TryAcquireAsync(IDatabaseAsync database) => TryAcquireReadScript.ExecuteAsync(database, this).AsBooleanTask();
        public bool TryAcquire(IDatabase database) => (bool)TryAcquireReadScript.Execute(database, this);
    }

    internal class RedisWriterWaitingPrimitive : RedisMutexPrimitive
    {
        public const string LockIdSuffix = "_WRITERWAITING";

        public RedisWriterWaitingPrimitive(RedisKey writerKey, RedisValue baseLockId, RedLockTimeouts timeouts)
            : base(writerKey, baseLockId + LockIdSuffix, timeouts)
        {
        }
    }

    internal class RedisWriteLockPrimitive : IRedLockAcquirableSynchronizationPrimitive, IRedLockExtensibleSynchronizationPrimitive
    {
        private readonly RedisKey _readerKey, _writerKey;
        private readonly RedisValue _lockId;
        private readonly RedLockTimeouts _timeouts;
        private readonly RedisMutexPrimitive _mutexPrimitive;

        public RedisWriteLockPrimitive(
            RedisKey readerKey, 
            RedisKey writerKey, 
            RedisValue lockId,
            RedLockTimeouts timeouts)
        {
            this._readerKey = readerKey;
            this._writerKey = writerKey;
            this._lockId = lockId;
            this._timeouts = timeouts;
            this._mutexPrimitive = new RedisMutexPrimitive(this._writerKey, this._lockId, this._timeouts);
        }

        public TimeoutValue AcquireTimeout => this._timeouts.AcquireTimeout;

        public void Release(IDatabase database, bool fireAndForget) => this._mutexPrimitive.Release(database, fireAndForget);
        public Task ReleaseAsync(IDatabaseAsync database, bool fireAndForget) => this._mutexPrimitive.ReleaseAsync(database, fireAndForget);

        /// <summary>
        /// TRY ACQUIRE WRITE
        /// 
        /// First, check if writerValue exists. If so, fail unless it's our waiting ID.
        /// 
        /// Then, check if there are no readers. If so, then set writerValue to our ID and return success. If not, then if the lock
        /// has our waiting ID re-up the expiry (avoids the need to extend the writer waiting lock).
        /// 
        /// Finally, return failure.
        /// </summary>
        private static readonly RedisScript<RedisWriteLockPrimitive> TryAcquireWriteScript = new RedisScript<RedisWriteLockPrimitive>($@"
            local writerValue = redis.call('get', @writerKey)
            if writerValue == false or writerValue == @lockId .. '{RedisWriterWaitingPrimitive.LockIdSuffix}' then
                if redis.call('scard', @readerKey) == 0 then
                    redis.call('set', @writerKey, @lockId, 'px', @expiryMillis)
                    return 1
                end
                if writerValue ~= false then
                    redis.call('pexpire', @writerKey, @expiryMillis)
                end
            end
            return 0",
            p => new { writerKey = p._writerKey, readerKey = p._readerKey, lockId = p._lockId, expiryMillis = p._timeouts.Expiry.InMilliseconds }
        );

        public bool TryAcquire(IDatabase database) => (bool)TryAcquireWriteScript.Execute(database, this);
        public Task<bool> TryAcquireAsync(IDatabaseAsync database) => TryAcquireWriteScript.ExecuteAsync(database, this).AsBooleanTask();

        public Task<bool> TryExtendAsync(IDatabaseAsync database) => this._mutexPrimitive.TryExtendAsync(database);
    }
}
