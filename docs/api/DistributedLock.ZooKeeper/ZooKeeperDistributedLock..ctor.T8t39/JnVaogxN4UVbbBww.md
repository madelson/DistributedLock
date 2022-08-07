#### [DistributedLock.ZooKeeper](README.md 'README')
### [Medallion.Threading.ZooKeeper](Medallion.Threading.ZooKeeper.md 'Medallion.Threading.ZooKeeper').[ZooKeeperDistributedLock](ZooKeeperDistributedLock.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedLock')

## ZooKeeperDistributedLock(ZooKeeperPath, string, bool, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>) Constructor

Constructs a new lock based on the provided [path](ZooKeeperDistributedLock..ctor.T8t39/JnVaogxN4UVbbBww.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedLock.ZooKeeperDistributedLock(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).path 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedLock.ZooKeeperDistributedLock(Medallion.Threading.ZooKeeper.ZooKeeperPath, string, bool, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).path'), [connectionString](ZooKeeperDistributedLock..ctor.T8t39/JnVaogxN4UVbbBww.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedLock.ZooKeeperDistributedLock(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).connectionString 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedLock.ZooKeeperDistributedLock(Medallion.Threading.ZooKeeper.ZooKeeperPath, string, bool, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).connectionString'), and [options](ZooKeeperDistributedLock..ctor.T8t39/JnVaogxN4UVbbBww.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedLock.ZooKeeperDistributedLock(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).options 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedLock.ZooKeeperDistributedLock(Medallion.Threading.ZooKeeper.ZooKeeperPath, string, bool, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).options').  
  
If [assumePathExists](ZooKeeperDistributedLock..ctor.T8t39/JnVaogxN4UVbbBww.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedLock.ZooKeeperDistributedLock(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).assumePathExists 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedLock.ZooKeeperDistributedLock(Medallion.Threading.ZooKeeper.ZooKeeperPath, string, bool, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).assumePathExists') is specified, then the node will not be created as part of acquiring nor will it be   
deleted after releasing (defaults to false).

```csharp
public ZooKeeperDistributedLock(Medallion.Threading.ZooKeeper.ZooKeeperPath path, string connectionString, bool assumePathExists=false, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedLock.ZooKeeperDistributedLock(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).path'></a>

`path` [ZooKeeperPath](ZooKeeperPath.md 'Medallion.Threading.ZooKeeper.ZooKeeperPath')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedLock.ZooKeeperDistributedLock(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).connectionString'></a>

`connectionString` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedLock.ZooKeeperDistributedLock(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).assumePathExists'></a>

`assumePathExists` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedLock.ZooKeeperDistributedLock(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[ZooKeeperDistributedSynchronizationOptionsBuilder](ZooKeeperDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')