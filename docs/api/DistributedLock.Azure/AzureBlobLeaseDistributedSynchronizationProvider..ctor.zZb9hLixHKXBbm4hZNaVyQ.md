#### [DistributedLock.Azure](README.md 'README')
### [Medallion.Threading.Azure](Medallion.Threading.Azure.md 'Medallion.Threading.Azure').[AzureBlobLeaseDistributedSynchronizationProvider](AzureBlobLeaseDistributedSynchronizationProvider.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedSynchronizationProvider')

## AzureBlobLeaseDistributedSynchronizationProvider(BlobContainerClient, Action<AzureBlobLeaseOptionsBuilder>) Constructor

Constructs a provider that scopes blobs within the provided [blobContainerClient](AzureBlobLeaseDistributedSynchronizationProvider..ctor.zZb9hLixHKXBbm4hZNaVyQ.md#Medallion.Threading.Azure.AzureBlobLeaseDistributedSynchronizationProvider.AzureBlobLeaseDistributedSynchronizationProvider(BlobContainerClient,System.Action_Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder_).blobContainerClient 'Medallion.Threading.Azure.AzureBlobLeaseDistributedSynchronizationProvider.AzureBlobLeaseDistributedSynchronizationProvider(BlobContainerClient, System.Action<Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder>).blobContainerClient') and uses the provided [options](AzureBlobLeaseDistributedSynchronizationProvider..ctor.zZb9hLixHKXBbm4hZNaVyQ.md#Medallion.Threading.Azure.AzureBlobLeaseDistributedSynchronizationProvider.AzureBlobLeaseDistributedSynchronizationProvider(BlobContainerClient,System.Action_Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder_).options 'Medallion.Threading.Azure.AzureBlobLeaseDistributedSynchronizationProvider.AzureBlobLeaseDistributedSynchronizationProvider(BlobContainerClient, System.Action<Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder>).options').

```csharp
public AzureBlobLeaseDistributedSynchronizationProvider(BlobContainerClient blobContainerClient, System.Action<Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.Azure.AzureBlobLeaseDistributedSynchronizationProvider.AzureBlobLeaseDistributedSynchronizationProvider(BlobContainerClient,System.Action_Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder_).blobContainerClient'></a>

`blobContainerClient` [Azure.Storage.Blobs.BlobContainerClient](https://docs.microsoft.com/en-us/dotnet/api/Azure.Storage.Blobs.BlobContainerClient 'Azure.Storage.Blobs.BlobContainerClient')

<a name='Medallion.Threading.Azure.AzureBlobLeaseDistributedSynchronizationProvider.AzureBlobLeaseDistributedSynchronizationProvider(BlobContainerClient,System.Action_Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[AzureBlobLeaseOptionsBuilder](AzureBlobLeaseOptionsBuilder.md 'Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')