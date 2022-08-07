#### [DistributedLock.Azure](README.md 'README')
### [Medallion.Threading.Azure](Medallion.Threading.Azure.md 'Medallion.Threading.Azure')

## AzureBlobLeaseDistributedSynchronizationProvider Class

Implements [IDistributedLockProvider](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockProvider.md 'Medallion.Threading.IDistributedLockProvider') for [AzureBlobLeaseDistributedLock](AzureBlobLeaseDistributedLock.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock')

```csharp
public sealed class AzureBlobLeaseDistributedSynchronizationProvider :
Medallion.Threading.IDistributedLockProvider
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; AzureBlobLeaseDistributedSynchronizationProvider

Implements [IDistributedLockProvider](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockProvider.md 'Medallion.Threading.IDistributedLockProvider')

| Constructors | |
| :--- | :--- |
| [AzureBlobLeaseDistributedSynchronizationProvider(BlobContainerClient, Action&lt;AzureBlobLeaseOptionsBuilder&gt;)](AzureBlobLeaseDistributedSynchronizationProvider..ctor.zZb9hLixHKXBbm4hZNaVyQ.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedSynchronizationProvider.AzureBlobLeaseDistributedSynchronizationProvider(BlobContainerClient, System.Action<Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder>)') | Constructs a provider that scopes blobs within the provided [blobContainerClient](AzureBlobLeaseDistributedSynchronizationProvider..ctor.zZb9hLixHKXBbm4hZNaVyQ.md#Medallion.Threading.Azure.AzureBlobLeaseDistributedSynchronizationProvider.AzureBlobLeaseDistributedSynchronizationProvider(BlobContainerClient,System.Action_Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder_).blobContainerClient 'Medallion.Threading.Azure.AzureBlobLeaseDistributedSynchronizationProvider.AzureBlobLeaseDistributedSynchronizationProvider(BlobContainerClient, System.Action<Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder>).blobContainerClient') and uses the provided [options](AzureBlobLeaseDistributedSynchronizationProvider..ctor.zZb9hLixHKXBbm4hZNaVyQ.md#Medallion.Threading.Azure.AzureBlobLeaseDistributedSynchronizationProvider.AzureBlobLeaseDistributedSynchronizationProvider(BlobContainerClient,System.Action_Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder_).options 'Medallion.Threading.Azure.AzureBlobLeaseDistributedSynchronizationProvider.AzureBlobLeaseDistributedSynchronizationProvider(BlobContainerClient, System.Action<Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder>).options'). |

| Methods | |
| :--- | :--- |
| [CreateLock(string)](AzureBlobLeaseDistributedSynchronizationProvider.CreateLock.I34EpC7k9lisV6FiK0xunw.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedSynchronizationProvider.CreateLock(string)') | Constructs an [AzureBlobLeaseDistributedLock](AzureBlobLeaseDistributedLock.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock') with the given [name](AzureBlobLeaseDistributedSynchronizationProvider.CreateLock.I34EpC7k9lisV6FiK0xunw.md#Medallion.Threading.Azure.AzureBlobLeaseDistributedSynchronizationProvider.CreateLock(string).name 'Medallion.Threading.Azure.AzureBlobLeaseDistributedSynchronizationProvider.CreateLock(string).name'). |
