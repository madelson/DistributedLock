# DistributedLock.MongoDB

[Download the NuGet package](https://www.nuget.org/packages/DistributedLock.MongoDB) [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.MongoDB.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.MongoDB/)

The DistributedLock.MongoDB package offers distributed locks based on [MongoDB](https://www.mongodb.com/). For example:

```C#
var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("myDatabase");
var @lock = new MongoDistributedLock("MyLockName", database);
await using (await @lock.AcquireAsync())
{
    // I have the lock
}
```

## APIs

- The `MongoDistributedLock` class implements the `IDistributedLock` interface.
- The `MongoDistributedSynchronizationProvider` class implements the `IDistributedLockProvider` interface.

## Implementation notes

MongoDB-based locks use MongoDB's document upsert and update operations to implement distributed locking. The implementation works as follows:

1. **Acquisition**: Attempts to insert or update a document with the lock key and a unique lock ID.
2. **Extension**: Automatically extends the lock expiry while held to prevent timeout.
3. **Release**: Deletes the lock document when disposed.
4. **Expiry**: Locks automatically expire if not extended, allowing recovery from crashed processes.

MongoDB locks can be constructed with an `IMongoDatabase` and an optional collection name. If no collection name is specified, locks will be stored in a collection named `"DistributedLocks"`. The collection will automatically have an index created on the `expiresAt` field for efficient queries.

When using the provider pattern, you can create multiple locks with different names from the same provider:

```C#
var client = new MongoClient(connectionString);
var database = client.GetDatabase("myDatabase");
var provider = new MongoDistributedSynchronizationProvider(database);

var lock1 = provider.CreateLock("lock1");
var lock2 = provider.CreateLock("lock2");

await using (await lock1.AcquireAsync())
{
    // Do work with lock1
}
```

**NOTE**: Lock extension happens automatically in the background while the lock is held. If lock extension fails (for example, due to network issues), the `HandleLostToken` will be signaled to notify you that the lock may have been lost.

## Options

In addition to specifying the name and database, several tuning options are available:

- `Expiry` determines how long the lock will be initially claimed for. Because of automatic extension, locks can be held for longer than this value. Defaults to 30 seconds.
- `ExtensionCadence` determines how frequently the hold on the lock will be renewed to the full `Expiry`. Defaults to 1/3 of `Expiry` (approximately 10 seconds when using the default expiry).
- `BusyWaitSleepTime` specifies a range of times that the implementation will sleep between attempts to acquire a lock that is currently held by someone else. A random time in the range will be chosen for each sleep. If you expect contention, lowering these values may increase responsiveness (how quickly a lock detects that it can now be taken) but will increase the number of calls made to MongoDB. Raising the values will have the reverse effects. Defaults to a range of 10ms to 800ms.

Example of using options:

```C#
var @lock = new MongoDistributedLock(
    "MyLockName",
    database,
    options => options
        .Expiry(TimeSpan.FromSeconds(30))
        .ExtensionCadence(TimeSpan.FromSeconds(10))
        .BusyWaitSleepTime(
            min: TimeSpan.FromMilliseconds(10),
            max: TimeSpan.FromMilliseconds(800))
);
```

You can also specify a custom collection name:

```C#
var @lock = new MongoDistributedLock("MyLockName", database, "MyCustomLocks");
```

## Stale lock cleanup

Stale locks from crashed processes will automatically expire based on the `Expiry` setting. MongoDB's built-in TTL index support ensures that expired lock documents are cleaned up automatically by the database. This means that if a process crashes while holding a lock, the lock will become available again after the expiry time has elapsed.
