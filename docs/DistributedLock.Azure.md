# DistributedLock.Azure

[Download the NuGet package](https://www.nuget.org/packages/DistributedLock.Azure) [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.Azure.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.Azure/)

The DistributedLock.Azure package offers distributed locks based on [Azure blob leases](https://docs.microsoft.com/en-us/rest/api/storageservices/lease-blob). For example:

```C#
var container = new BlobContainerClient(myAzureConnectionString, "my-locking-container-name");
var @lock = new AzureBlobLeaseDistributedLock(container, "MyLockName");
await using (var handle = await @lock.TryAcquireAsync())
{
  if (handle != null) { /* I have the lock */ }
}
```

## APIs

- The `AzureBlobLeaseDistributedLock` class implements the `IDistributedLock` interface.
- The `AzureBlobLeaseDistributedSynchronizationProvider` class implements the `IDistributedLockProvider` interface.

## Implementation notes

`AzureBlobLeaseDistributedLock`s can be constructed either from a `BlobContainerClient` and a name, which will cause it to lease a blob in the provided container with a name based on the provided name. If you know exactly which blob you'd like to lease, another constructor lets you pass a `BlobBaseClient` instead.

Because of how Azure leases work, the acquire operation cannot truly block. If waiting to acquire a lock that is not available, the implementation will periodically sleep and retry until the lease can be taken or the acquire timeout elapses. Because of this, these locks are maximally efficient when using `TryAcquire` semantics with a timeout of zero.

Blob leases in Azure have built-in expirations. However while an `AzureBlobLeaseDistributedLock` is held it will periodically renew the lease in the background. Therefore, it is generally safe to ignore the problem of lease duration.

## Options

In addition to specifying the blob to be leased, several tuning options are provided. You should not need to change these options most of the time.

- `Duration` changes the blob lease duration requested under the hood
- `RenewalCadence` changes how frequently auto-renewal re-ups the lease duration while holding the lock
- `BusyWaitSleepTime` specifies a range of times that the implementation will sleep between attempts to acquire a lease that is currently held by someone else. A random number in the range will be chosen for each sleep. If you expect contention, lowering these values may increase the responsiveness (how quickly a lock detects that it can now take the lease) but will increase the number of API calls made to Azure. Raising the values will have the reverse effects.



