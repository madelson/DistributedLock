#### [DistributedLock.Azure](README.md 'README')
### [Medallion.Threading.Azure](Medallion.Threading.Azure.md 'Medallion.Threading.Azure').[AzureBlobLeaseDistributedLock](AzureBlobLeaseDistributedLock.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock')

## AzureBlobLeaseDistributedLock.AcquireAsync(Nullable<TimeSpan>, CancellationToken) Method

Acquires the lock asynchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage:   
  
```csharp  
await using (await myLock.AcquireAsync(...))  
{  
    /* we have the lock! */  
}  
// dispose releases the lock  
```

```csharp
public System.Threading.Tasks.ValueTask<Medallion.Threading.Azure.AzureBlobLeaseDistributedLockHandle> AcquireAsync(System.Nullable<System.TimeSpan> timeout=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AcquireAsync(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')[System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')

How long to wait before giving up on the acquisition attempt. Defaults to [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan')

<a name='Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AcquireAsync(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [AcquireAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLock.AcquireAsync.0Lol7Hv58Kl+UVYSOI6IpQ.md 'Medallion.Threading.IDistributedLock.AcquireAsync(System.Nullable{System.TimeSpan},System.Threading.CancellationToken)')

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[AzureBlobLeaseDistributedLockHandle](AzureBlobLeaseDistributedLockHandle.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLockHandle')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')  
An [AzureBlobLeaseDistributedLockHandle](AzureBlobLeaseDistributedLockHandle.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLockHandle') which can be used to release the lock