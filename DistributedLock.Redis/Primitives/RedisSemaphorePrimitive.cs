using Medallion.Threading.Internal;
using Medallion.Threading.Redis.RedLock;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Redis.Primitives;

/// <summary>
/// The semaphore algorithm looks similar to the mutex implementation except that the value stored at the key is a
/// sorted set (sorted by timeout). Because elements aren't automatically removed from the set when they time out,
/// would-be acquirers must first purge the set of any expired values before they check whether the set has space
/// for them.
/// </summary>
internal class RedisSemaphorePrimitive : IRedLockAcquirableSynchronizationPrimitive, IRedLockExtensibleSynchronizationPrimitive
{
    // replicate_commands is necessary to call before calling non-deterministic functions
    private const string GetNowMillisScriptFragment = @"
            redis.replicate_commands()
            local nowResult = redis.call('time')
            local nowMillis = (tonumber(nowResult[1]) * 1000.0) + (tonumber(nowResult[2]) / 1000.0)";

    private const string RenewSetScriptFragment = @"
            local keyTtl = redis.call('pttl', @key)
            if keyTtl < tonumber(@setExpiryMillis) then
                redis.call('pexpire', @key, @setExpiryMillis)
            end";

    private readonly RedisValue _lockId = RedLockHelper.CreateLockId();
    private readonly RedisKey _key;
    private readonly int _maxCount;
    private readonly RedLockTimeouts _timeouts;

    public RedisSemaphorePrimitive(RedisKey key, int maxCount, RedLockTimeouts timeouts)
    {
        this._key = key;
        this._maxCount = maxCount;
        this._timeouts = timeouts;
    }

    public TimeoutValue AcquireTimeout => this._timeouts.AcquireTimeout;

    /// <summary>
    /// The actual expiry is determined by the entry in the timeouts set. However, we also don't want to pollute the db by leaving
    /// the sets around forever. Therefore, we give the sets an expiry of 3x the individual entry expiry. The reason to be extra
    /// conservative with sets is that there is more disruption from losing them then from having one key time out.
    /// </summary>
    private TimeoutValue SetExpiry => TimeSpan.FromMilliseconds((int)Math.Min(int.MaxValue, 3L * this._timeouts.Expiry.InMilliseconds));

    public void Release(IDatabase database, bool fireAndForget) =>
        database.SortedSetRemove(this._key, this._lockId, RedLockHelper.GetCommandFlags(fireAndForget));

    public Task ReleaseAsync(IDatabaseAsync database, bool fireAndForget) =>
        database.SortedSetRemoveAsync(this._key, this._lockId, RedLockHelper.GetCommandFlags(fireAndForget));

    private static readonly RedisScript<RedisSemaphorePrimitive> AcquireScript = new RedisScript<RedisSemaphorePrimitive>($@"
            {GetNowMillisScriptFragment}
            redis.call('zremrangebyscore', @key, '-inf', nowMillis)
            if redis.call('zcard', @key) < tonumber(@maxCount) then
                redis.call('zadd', @key, nowMillis + tonumber(@expiryMillis), @lockId)
                {RenewSetScriptFragment}
                return 1
            end
            return 0",
        p => new { key = p._key, maxCount = p._maxCount, expiryMillis = p._timeouts.Expiry.InMilliseconds, lockId = p._lockId, setExpiryMillis = p.SetExpiry.InMilliseconds }
    );

    public bool TryAcquire(IDatabase database) => (bool)AcquireScript.Execute(database, this);

    public Task<bool> TryAcquireAsync(IDatabaseAsync database) => AcquireScript.ExecuteAsync(database, this).AsBooleanTask();

    private static readonly RedisScript<RedisSemaphorePrimitive> ExtendScript = new RedisScript<RedisSemaphorePrimitive>($@"
            {GetNowMillisScriptFragment}
            local result = redis.call('zadd', @key, 'XX', 'CH', nowMillis + tonumber(@expiryMillis), @lockId)
            {RenewSetScriptFragment}
            return result",
        p => new { key = p._key, expiryMillis = p._timeouts.Expiry.InMilliseconds, lockId = p._lockId, setExpiryMillis = p.SetExpiry.InMilliseconds }
    );

    public Task<bool> TryExtendAsync(IDatabaseAsync database) => ExtendScript.ExecuteAsync(database, this).AsBooleanTask();
}
