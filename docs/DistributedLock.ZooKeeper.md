# DistributedLock.ZooKeeper

[Download the NuGet package](https://www.nuget.org/packages/DistributedLock.ZooKeeper) [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.ZooKeeper.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.ZooKeeper/)

The DistributedLock.ZooKeeper package offers distributed locks based on [Apache ZooKeeper](https://zookeeper.apache.org/). For example:

```C#
var @lock = new ZooKeeperDistributedLock("MyLockName", connectionString);
await using (await @lock.AcquireAsync())
{
  // I have the lock
}
```

## APIs

- The `ZooKeeperDistributedLock` class implements the `IDistributedLock` interface.
- The `ZooKeeperDistributedReaderWriterLock` class implements the `IDistributedReaderWriterLock` interface.
- The `ZooKeeperDistributedSemaphore` class implements the `IDistributedSemaphore` interface.
- The `ZooKeeperDistributedSynchronizationProvider` class implements the `IDistributedLockProvider`, `IDistributedReaderWriterLockProvider`, and `IDistributedSemaphoreProvider` interfaces.

## Implementation notes

ZooKeeper-based locks leverage ZooKeeper's recommended [recipes](https://zookeeper.apache.org/doc/r3.1.2/recipes.html) for distributed synchronization.

By leveraging ZooKeeper watches under the hood, these recipes allow for very efficient event-driven waits when acquiring.

## Options

- `SessionTimeout` configures the underlying session timeout value for ZooKeeper connections. Because the underlying ZooKeeper client periodically renews the session, this value generally will not impact behavior. Lower values mean that locks will be released more quickly following a crash of the lock-holding process, but also increase the risk that transient connection issues will result in a dropped lock. Defaults to 20s.
- `ConnectTimeout` configures how long to wait when establishing a connection to ZooKeeper. Defaults to 15s.
- `AddAuthInfo` allows you to specify additional auth information to be added to the ZooKeeper session. This option can be specified multiple times to add multiple auth schemes.
- `AddAccessControl` configures the ZooKeeper ACL for nodes created by the lock. This option can be specified multiple times to add multiple ACL entries. If left unspecified, the ACL used is (world, anyone).

