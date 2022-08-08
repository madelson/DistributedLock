#### [DistributedLock.ZooKeeper](README.md 'README')
### [Medallion.Threading.ZooKeeper](Medallion.Threading.ZooKeeper.md 'Medallion.Threading.ZooKeeper').[ZooKeeperDistributedSemaphore](ZooKeeperDistributedSemaphore.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore')

## ZooKeeperDistributedSemaphore(string, int, string, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>) Constructor

Constructs a new semaphore based on the provided [name](ZooKeeperDistributedSemaphore..ctor.ceNJY9cjGCQ/TgjjnsQT9A.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(string,int,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).name 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(string, int, string, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).name'), [connectionString](ZooKeeperDistributedSemaphore..ctor.ceNJY9cjGCQ/TgjjnsQT9A.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(string,int,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).connectionString 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(string, int, string, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).connectionString'), and [options](ZooKeeperDistributedSemaphore..ctor.ceNJY9cjGCQ/TgjjnsQT9A.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(string,int,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).options 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(string, int, string, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).options').

The semaphore's path will be a parent node of the root directory '/'. If [name](ZooKeeperDistributedSemaphore..ctor.ceNJY9cjGCQ/TgjjnsQT9A.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(string,int,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).name 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(string, int, string, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).name') is not a valid node name, it will be transformed to ensure
validity.

```csharp
public ZooKeeperDistributedSemaphore(string name, int maxCount, string connectionString, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(string,int,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(string,int,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).maxCount'></a>

`maxCount` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(string,int,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).connectionString'></a>

`connectionString` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(string,int,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[ZooKeeperDistributedSynchronizationOptionsBuilder](ZooKeeperDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')