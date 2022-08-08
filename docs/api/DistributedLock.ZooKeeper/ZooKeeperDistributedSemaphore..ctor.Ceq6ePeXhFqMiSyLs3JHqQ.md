#### [DistributedLock.ZooKeeper](README.md 'README')
### [Medallion.Threading.ZooKeeper](Medallion.Threading.ZooKeeper.md 'Medallion.Threading.ZooKeeper').[ZooKeeperDistributedSemaphore](ZooKeeperDistributedSemaphore.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore')

## ZooKeeperDistributedSemaphore(ZooKeeperPath, int, string, bool, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>) Constructor

Constructs a new semaphore based on the provided [path](ZooKeeperDistributedSemaphore..ctor.Ceq6ePeXhFqMiSyLs3JHqQ.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(Medallion.Threading.ZooKeeper.ZooKeeperPath,int,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).path 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(Medallion.Threading.ZooKeeper.ZooKeeperPath, int, string, bool, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).path'), [connectionString](ZooKeeperDistributedSemaphore..ctor.Ceq6ePeXhFqMiSyLs3JHqQ.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(Medallion.Threading.ZooKeeper.ZooKeeperPath,int,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).connectionString 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(Medallion.Threading.ZooKeeper.ZooKeeperPath, int, string, bool, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).connectionString'), and [options](ZooKeeperDistributedSemaphore..ctor.Ceq6ePeXhFqMiSyLs3JHqQ.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(Medallion.Threading.ZooKeeper.ZooKeeperPath,int,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).options 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(Medallion.Threading.ZooKeeper.ZooKeeperPath, int, string, bool, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).options').

If [assumePathExists](ZooKeeperDistributedSemaphore..ctor.Ceq6ePeXhFqMiSyLs3JHqQ.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(Medallion.Threading.ZooKeeper.ZooKeeperPath,int,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).assumePathExists 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(Medallion.Threading.ZooKeeper.ZooKeeperPath, int, string, bool, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).assumePathExists') is specified, then the node will not be created as part of acquiring nor will it be 
deleted after releasing (defaults to false).

```csharp
public ZooKeeperDistributedSemaphore(Medallion.Threading.ZooKeeper.ZooKeeperPath path, int maxCount, string connectionString, bool assumePathExists=false, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(Medallion.Threading.ZooKeeper.ZooKeeperPath,int,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).path'></a>

`path` [ZooKeeperPath](ZooKeeperPath.md 'Medallion.Threading.ZooKeeper.ZooKeeperPath')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(Medallion.Threading.ZooKeeper.ZooKeeperPath,int,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).maxCount'></a>

`maxCount` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(Medallion.Threading.ZooKeeper.ZooKeeperPath,int,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).connectionString'></a>

`connectionString` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(Medallion.Threading.ZooKeeper.ZooKeeperPath,int,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).assumePathExists'></a>

`assumePathExists` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore.ZooKeeperDistributedSemaphore(Medallion.Threading.ZooKeeper.ZooKeeperPath,int,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[ZooKeeperDistributedSynchronizationOptionsBuilder](ZooKeeperDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')