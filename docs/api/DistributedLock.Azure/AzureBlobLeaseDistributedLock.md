#### [DistributedLock.Azure](README.md 'README')
### [Medallion.Threading.Azure](Medallion.Threading.Azure.md 'Medallion.Threading.Azure')

## AzureBlobLeaseDistributedLock Class

Implements a [IDistributedLock](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLock.md 'Medallion.Threading.IDistributedLock') based on Azure blob leases

```csharp
public sealed class AzureBlobLeaseDistributedLock :
Medallion.Threading.IDistributedLock
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; AzureBlobLeaseDistributedLock

Implements [IDistributedLock](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLock.md 'Medallion.Threading.IDistributedLock')

| Constructors | |
| :--- | :--- |
| [AzureBlobLeaseDistributedLock(BlobBaseClient, Action&lt;AzureBlobLeaseOptionsBuilder&gt;)](AzureBlobLeaseDistributedLock..ctor.PPb+8c/p7LTz4wSm4vPWGg.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobBaseClient, System.Action<Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder>)') | Constructs a lock that will lease the provided [blobClient](AzureBlobLeaseDistributedLock..ctor.PPb+8c/p7LTz4wSm4vPWGg.md#Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobBaseClient,System.Action_Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder_).blobClient 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobBaseClient, System.Action<Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder>).blobClient') |
| [AzureBlobLeaseDistributedLock(BlobContainerClient, string, Action&lt;AzureBlobLeaseOptionsBuilder&gt;)](AzureBlobLeaseDistributedLock..ctor.5tADfQ9q5nXUDj477SWvHg.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobContainerClient, string, System.Action<Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder>)') | Constructs a lock that will lease a blob based on [name](AzureBlobLeaseDistributedLock..ctor.5tADfQ9q5nXUDj477SWvHg.md#Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobContainerClient,string,System.Action_Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder_).name 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobContainerClient, string, System.Action<Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder>).name') within the provided [blobContainerClient](AzureBlobLeaseDistributedLock..ctor.5tADfQ9q5nXUDj477SWvHg.md#Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobContainerClient,string,System.Action_Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder_).blobContainerClient 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobContainerClient, string, System.Action<Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder>).blobContainerClient'). |

| Properties | |
| :--- | :--- |
| [Name](AzureBlobLeaseDistributedLock.Name.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.Name') | Implements [Name](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLock.Name.md 'Medallion.Threading.IDistributedLock.Name') |

| Methods | |
| :--- | :--- |
| [Acquire(Nullable&lt;TimeSpan&gt;, CancellationToken)](AzureBlobLeaseDistributedLock.Acquire.jrwmxrsi+eqzAdB3xMnaMw.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.Acquire(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires the lock synchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: |
| [AcquireAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](AzureBlobLeaseDistributedLock.AcquireAsync.ulCFSvmk3VUn6f5ebzKW9A.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AcquireAsync(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires the lock asynchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: |
| [TryAcquire(TimeSpan, CancellationToken)](AzureBlobLeaseDistributedLock.TryAcquire.1mh2rZwMkBLLsl/Ri/nDJA.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.TryAcquire(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire the lock synchronously. Usage: |
| [TryAcquireAsync(TimeSpan, CancellationToken)](AzureBlobLeaseDistributedLock.TryAcquireAsync.h3ZvrsNxi9ZoUPGAFzUdCQ.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.TryAcquireAsync(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire the lock asynchronously. Usage: |
