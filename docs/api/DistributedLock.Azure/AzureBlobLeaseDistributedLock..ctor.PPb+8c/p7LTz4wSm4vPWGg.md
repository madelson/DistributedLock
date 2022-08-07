#### [DistributedLock.Azure](README.md 'README')
### [Medallion.Threading.Azure](Medallion.Threading.Azure.md 'Medallion.Threading.Azure').[AzureBlobLeaseDistributedLock](AzureBlobLeaseDistributedLock.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock')

## AzureBlobLeaseDistributedLock(BlobBaseClient, Action<AzureBlobLeaseOptionsBuilder>) Constructor

Constructs a lock that will lease the provided [blobClient](AzureBlobLeaseDistributedLock..ctor.PPb+8c/p7LTz4wSm4vPWGg.md#Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobBaseClient,System.Action_Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder_).blobClient 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobBaseClient, System.Action<Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder>).blobClient')

```csharp
public AzureBlobLeaseDistributedLock(BlobBaseClient blobClient, System.Action<Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobBaseClient,System.Action_Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder_).blobClient'></a>

`blobClient` [Azure.Storage.Blobs.Specialized.BlobBaseClient](https://docs.microsoft.com/en-us/dotnet/api/Azure.Storage.Blobs.Specialized.BlobBaseClient 'Azure.Storage.Blobs.Specialized.BlobBaseClient')

<a name='Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobBaseClient,System.Action_Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[AzureBlobLeaseOptionsBuilder](AzureBlobLeaseOptionsBuilder.md 'Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')