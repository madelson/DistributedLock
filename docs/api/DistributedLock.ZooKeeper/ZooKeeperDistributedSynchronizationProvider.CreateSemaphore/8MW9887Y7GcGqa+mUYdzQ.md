#### [DistributedLock.ZooKeeper](README.md 'README')
### [Medallion.Threading.ZooKeeper](Medallion.Threading.ZooKeeper.md 'Medallion.Threading.ZooKeeper').[ZooKeeperDistributedSynchronizationProvider](ZooKeeperDistributedSynchronizationProvider.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider')

## ZooKeeperDistributedSynchronizationProvider.CreateSemaphore(string, int) Method

Creates a [ZooKeeperDistributedSemaphore](ZooKeeperDistributedSemaphore.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore') using the given [name](ZooKeeperDistributedSynchronizationProvider.CreateSemaphore./8MW9887Y7GcGqa+mUYdzQ.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.CreateSemaphore(string,int).name 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.CreateSemaphore(string, int).name') and [maxCount](ZooKeeperDistributedSynchronizationProvider.CreateSemaphore./8MW9887Y7GcGqa+mUYdzQ.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.CreateSemaphore(string,int).maxCount 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.CreateSemaphore(string, int).maxCount').

```csharp
public Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore CreateSemaphore(string name, int maxCount);
```
#### Parameters

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.CreateSemaphore(string,int).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.CreateSemaphore(string,int).maxCount'></a>

`maxCount` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

Implements [CreateSemaphore(string, int)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSemaphoreProvider.CreateSemaphore.AA9FahTKczyqDQd0GIAGzQ.md 'Medallion.Threading.IDistributedSemaphoreProvider.CreateSemaphore(System.String,System.Int32)')

#### Returns
[ZooKeeperDistributedSemaphore](ZooKeeperDistributedSemaphore.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore')