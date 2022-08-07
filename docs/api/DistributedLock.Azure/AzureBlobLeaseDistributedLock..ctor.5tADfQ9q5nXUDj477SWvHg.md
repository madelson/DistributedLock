#### [DistributedLock.Azure](README.md 'README')
### [Medallion.Threading.Azure](Medallion.Threading.Azure.md 'Medallion.Threading.Azure').[AzureBlobLeaseDistributedLock](AzureBlobLeaseDistributedLock.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock')

## AzureBlobLeaseDistributedLock(BlobContainerClient, string, Action<AzureBlobLeaseOptionsBuilder>) Constructor

Constructs a lock that will lease a blob based on [name](AzureBlobLeaseDistributedLock..ctor.5tADfQ9q5nXUDj477SWvHg.md#Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobContainerClient,string,System.Action_Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder_).name 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobContainerClient, string, System.Action<Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder>).name') within the provided [blobContainerClient](AzureBlobLeaseDistributedLock..ctor.5tADfQ9q5nXUDj477SWvHg.md#Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobContainerClient,string,System.Action_Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder_).blobContainerClient 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobContainerClient, string, System.Action<Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder>).blobContainerClient').

```csharp
public AzureBlobLeaseDistributedLock(BlobContainerClient blobContainerClient, string name, System.Action<Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobContainerClient,string,System.Action_Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder_).blobContainerClient'></a>

`blobContainerClient` [Azure.Storage.Blobs.BlobContainerClient](https://docs.microsoft.com/en-us/dotnet/api/Azure.Storage.Blobs.BlobContainerClient 'Azure.Storage.Blobs.BlobContainerClient')

<a name='Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobContainerClient,string,System.Action_Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder_).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.AzureBlobLeaseDistributedLock(BlobContainerClient,string,System.Action_Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[AzureBlobLeaseOptionsBuilder](AzureBlobLeaseOptionsBuilder.md 'Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')