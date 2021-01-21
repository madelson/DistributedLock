# DistributedLock.Redis

[Download the NuGet package](https://www.nuget.org/packages/DistributedLock.Redis) [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.Redis.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.Redis/)

The DistributedLock.Redis package offers distributed synchronization primitives based on [Redis](https://redis.io/). For example:

```C#
var connection = await ConnectionMultiplexer.ConnectAsync(connectionString); // uses StackExchange.Redis
var @lock = new RedisDistributedLock("MyLockName", connection.GetDatabase());
await using (var handle = await @lock.TryAcquireAsync())
{
    if (handle != null) { /* I have the lock */ }
}
```

## APIs

- The `RedisDistributedLock` class implements the `IDistributedLock` interface.
- The `RedisDistributedReaderWriterLock` class implements the `IDistributedReaderWriterLock` interface
- The `RedisDistributedSemaphore` class implements the `IDistributedSemaphore` interface
- The `RedisDistributedSynchronizationProvider` class implements the `IDistributedLockProvider`, `IDistributedReaderWriterLockProvider`, and `IDistributedSemaphoreProvider` interfaces.

## Implementation notes

The `RedisDistributedLock` and `RedisDistributedReaderWriterLock` classes implement the [RedLock algorithm](https://redis.io/topics/distlock). This allows you to increase the robustness of those locks by constructing the lock with a set of databases instead of just a single database. The lock is only considered aquired if it is successfully acquired on more than half of the databases.

The `RedisDistributedSemaphore` implementation is loosely based on [this algorithm](https://redislabs.com/ebook/part-2-core-concepts/chapter-6-application-components-in-redis/6-3-counting-semaphores/). Note that `RedisDistributedSemaphore` does not support multiple databases, because the RedLock algorithm does not work with semaphores.<sup>1</sup> When calling `CreateSemaphore()` on a `RedisDistributedSynchronizationProvider` that has been constructed with multiple databases, the first database in the list will be used.

Both RedLock and the semaphore algorithm mentioned above claim locks for only a specified period of time. While DistributedLock does this under the hood, it also periodically extends its hold behind the scenes to ensure that the object is not released until the handle returned by `Acquire` is disposed.

Some Redis synchronization primitives take in a `string name` as their name and others take in a `RedisKey key`. In the former case, one or more Redis keys will be created on the database with `name` as a prefix. In the latter case, the exact key will be used. Make sure your names/keys don't collide with Redis keys you're using for other purposes!

Because of how Redis locks work, the acquire operation cannot truly block. If waiting to acquire a lock or other primitive that is not available, the implementation will periodically sleep and retry until the lease can be taken or the acquire timeout elapses. Because of this, these classes are maximally efficient when using `TryAcquire` semantics with a timeout of zero.

## Options

In addition to specifying the name/key and database(s), some additional tuning options are available.

- `Expiry` determines how long the lock will be *initially* claimed for (because of auto-extension, locks can be held for longer). Defaults to 30s.
- `ExtensionCadence` determines how frequently the hold on the lock will be renewed to the full `Expiry`. Defaults to 1/3 of `Expiry`.
- `MinValidityTime` determines what fraction of `Expiry` still has to remain when the locking operation completes to consider it a success. This is mostly relevant when acquiring a lock across multiple databases (e. g. if we immediately succeed on database 1 and eventually succeed on database 2 after 30s have elapsed, then our hold on database 1 will have expired). Defaults to 90% of the `Expiry`.
- `BusyWaitSleepTime` specifies a range of times that the implementation will sleep between attempts to acquire a lock that is currently held by someone else. A random number in the range will be chosen for each sleep. If you expect contention, lowering these values may increase the responsiveness (how quickly a lock detects that it can now be taken) but will increase the number of calls made to REdis. Raising the values will have the reverse effects.


<sup>1</sup> <sub><sup>The reason RedLock does not work with semaphores is that entering a semaphore on a majority of databases does not guarantee that the semaphore's invariant is preserved. For example, imagine a two-count semaphore with three databases (1, 2, and 3) and three users (A, B, and C). We could find ourselves in the following situation: on database 1, users A and B have entered. On database 2, users B and C have entered. On database 3, users A and C have entered. Here all users believe they have entered the semaphore because they've succeeded on two out of three databases.</sup></sub>
