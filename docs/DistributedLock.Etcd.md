# DistributedLock.Etcd

[Download the NuGet package](https://www.nuget.org/packages/DistributedLock.Etcd) [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.Etcd.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.Etcd/)

The DistributedLock.Etcd package offers distributed locks based on [etcd](https://etcd.io/). For example:

```C#
using dotnet_etcd;
using Medallion.Threading.Etcd;

var client = new EtcdClient("http://localhost:2379");
var @lock = new EtcdLeaseDistributedLock(client, "MyLockName");
await using (await @lock.AcquireAsync())
{
    // I have the lock
}
```

## APIs

- The `EtcdLeaseDistributedLock` class implements the `IDistributedLock` interface.
- The `EtcdLeaseDistributedLockProvider` class implements the `IDistributedLockProvider` interface.

## Implementation notes

The `EtcdLeaseDistributedLock` implementation leverages etcd's lease mechanism and distributed lock primitives. It uses etcd's built-in lease functionality to automatically manage lock expiration and renewal.

The implementation creates a lease with a specified duration and uses that lease to acquire a distributed lock. The lease is automatically renewed in the background to ensure the lock remains held until explicitly released. This provides automatic cleanup in case the process holding the lock crashes or becomes unresponsive.

Because of how etcd locks work, the acquire operation cannot truly block. If waiting to acquire a lock that is not available, the implementation will periodically sleep and retry until the lock can be taken or the acquire timeout elapses. Because of this, these classes are maximally efficient when using `TryAcquire` semantics with a timeout of zero.

## Options

In addition to specifying the lock name and etcd client, some additional tuning options are available through the `EtcdLeaseOptionsBuilder`:

- `Duration` determines how long the lease will be initially claimed for (because of auto-renewal, locks can be held for longer). Must be between 15 and 60 seconds, or infinite. Defaults to 30s.
- `RenewalCadence` determines how frequently the hold on the lock will be renewed to the full `Duration`. Defaults to 1/3 of `Duration`. To disable auto-renewal, specify `Timeout.InfiniteTimeSpan`.
- `BusyWaitSleepTime` specifies a range of times that the implementation will sleep between attempts to acquire a lock that is currently held by someone else. A random number in the range will be chosen for each sleep. Lower values increase responsiveness but increase the number of calls made to etcd. The default is [250ms, 1s].

## Example with options

```C#
using dotnet_etcd;
using Medallion.Threading.Etcd;

var client = new EtcdClient("http://localhost:2379");
var @lock = new EtcdLeaseDistributedLock(client, "MyLockName", options =>
{
    options.Duration(TimeSpan.FromSeconds(45))
           .RenewalCadence(TimeSpan.FromSeconds(15))
           .BusyWaitSleepTime(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500));
});

await using (await @lock.AcquireAsync())
{
    // I have the lock with custom options
}
```

## Using the provider

```C#
using dotnet_etcd;
using Medallion.Threading.Etcd;

var client = new EtcdClient("http://localhost:2379");
var provider = new EtcdLeaseDistributedLockProvider(client);

var @lock = provider.CreateLock("MyLockName");
await using (await @lock.AcquireAsync())
{
    // I have the lock
}
``` 