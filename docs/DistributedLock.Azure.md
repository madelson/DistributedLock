# DistributedLock.Azure

[Download the NuGet package](https://www.nuget.org/packages/DistributedLock.Azure) [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.Azure.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.Azure/)

The DistributedLock.Azure package offers distributed locks based on [Azure blob leases](https://docs.microsoft.com/en-us/rest/api/storageservices/lease-blob) or on [Azure table storage](https://learn.microsoft.com/en-us/rest/api/storageservices/table-service-rest-api) entities. For example:

```C#
var container = new BlobContainerClient(myAzureConnectionString, "my-locking-container-name");
var @lock = new AzureBlobLeaseDistributedLock(container, "MyLockName");
await using (var handle = await @lock.TryAcquireAsync())
{
  if (handle != null) { /* I have the lock */ }
}
```

Or, using table storage:

```C#
var table = new TableClient(myAzureConnectionString, "MyLockingTableName");
var @lock = new AzureTableDistributedLock(table, "MyLockName");
await using (var handle = await @lock.TryAcquireAsync())
{
  if (handle != null) { /* I have the lock */ }
}
```

## APIs

- The `AzureBlobLeaseDistributedLock` class implements the `IDistributedLock` interface.
- The `AzureBlobLeaseDistributedSynchronizationProvider` class implements the `IDistributedLockProvider` interface.
- The `AzureTableDistributedLock` class implements the `IDistributedLock` interface.
- The `AzureTableDistributedSynchronizationProvider` class implements the `IDistributedLockProvider` interface.

## Blob leases vs. table storage

Both implementations offer the same API surface; the difference is in how mutual exclusion is enforced.

Blob leases are enforced by Azure itself: once a lease is taken, the service rejects conflicting operations regardless of which client is making them. Table locks are instead simulated with optimistic concurrency, using a row per lock, an `ExpiresAt` column, and ETag compare-and-swap. This makes them *cooperative*: they only exclude clients that go through this library, and expiration is evaluated against each client's own clock, so clock skew between machines directly affects correctness.

In exchange, table locks are not bound by the blob lease service constraints. Lease duration is not restricted to the 15-60 second range, the partition and row keys are fully customizable, and a released lock leaves no row behind.

Cost is the other consideration. Both implementations are transaction-heavy by nature: waiting for a contended lock means a request per busy-wait iteration, and holding one means a renewal request per `RenewalCadence`. On StorageV2 accounts, table transactions are typically billed at a meaningfully lower rate per operation than blob transactions, so a workload with a high lock volume may be noticeably cheaper on tables. Rates vary by region, redundancy and access tier, so check current pricing against your own numbers rather than assuming a fixed ratio.

If cost is a concern regardless of which implementation you choose, the biggest lever is usually the number of requests rather than the per-request price. Raising `BusyWaitSleepTime` reduces polling under contention, and raising `Duration` (which raises the default `RenewalCadence` with it) reduces renewals while a lock is held. Using `TryAcquire` with a zero timeout avoids busy-waiting altogether.

Prefer blob leases when correctness under adversarial or non-library access matters, since service-enforced leases hold regardless of who is calling. Prefer tables when you need the added flexibility or when transaction cost at your lock volume is significant.

## Implementation notes

`AzureBlobLeaseDistributedLock`s can be constructed either from a `BlobContainerClient` and a name, which will cause it to lease a blob in the provided container with a name based on the provided name. If you know exactly which blob you'd like to lease, another constructor lets you pass a `BlobBaseClient` instead.

Because of how Azure leases work, the acquire operation cannot truly block. If waiting to acquire a lock that is not available, the implementation will periodically sleep and retry until the lease can be taken or the acquire timeout elapses. Because of this, these locks are maximally efficient when using `TryAcquire` semantics with a timeout of zero. The same is true of table locks.

Blob leases in Azure have built-in expirations. However while an `AzureBlobLeaseDistributedLock` is held it will periodically renew the lease in the background. Therefore, it is generally safe to ignore the problem of lease duration. `AzureTableDistributedLock` renews in the same way.

`AzureTableDistributedLock`s are constructed from a `TableClient` and a name. By default all locks live in a single partition under a row key derived from the name, and the table is expected to already exist; see the options below to change either. Releasing a lock deletes its row, so tables do not accumulate rows for locks that are not currently held.

## Options

In addition to specifying the blob to be leased, several tuning options are provided. You should not need to change these options most of the time.

- `Duration` changes the blob lease duration requested under the hood
- `RenewalCadence` changes how frequently auto-renewal re-ups the lease duration while holding the lock
- `BusyWaitSleepTime` specifies a range of times that the implementation will sleep between attempts to acquire a lease that is currently held by someone else. A random number in the range will be chosen for each sleep. If you expect contention, lowering these values may increase the responsiveness (how quickly a lock detects that it can now take the lease) but will increase the number of API calls made to Azure. Raising the values will have the reverse effects.

`AzureTableDistributedLock` supports `Duration`, `RenewalCadence` and `BusyWaitSleepTime` with the same meanings, plus the following:

- `PartitionKey` and `RowKey` control how the backing entity's keys are derived from the lock name. Each accepts either a template string, in which `{name}` is replaced by the lock name, or a `Func<string, string>` for arbitrary conventions. A custom key is validated rather than sanitized, so a selector that returns an invalid key throws instead of silently locking on a different key than you expect. Note that a constant `RowKey` would cause every lock to collide on a single entity.
- `CreateTableIfNotExists` creates the table and retries once if an acquire fails because the table does not exist. This defaults to false, since creating tables is a control-plane operation that the credentials in use may not be permitted to perform.
- `AcquireStrategy` controls whether each attempt probes with an insert or a read first. The default, `Adaptive`, inserts first and switches to reading once contention is observed. You can also supply a constant, or a function of `AzureTableAcquireContext` that is evaluated before every attempt, which is useful if your lock naming convention tells you up front that a lock is likely to be contended.



