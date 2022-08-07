#### [DistributedLock.ZooKeeper](README.md 'README')
### [Medallion.Threading.ZooKeeper](Medallion.Threading.ZooKeeper.md 'Medallion.Threading.ZooKeeper').[ZooKeeperDistributedSynchronizationProvider](ZooKeeperDistributedSynchronizationProvider.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider')

## ZooKeeperDistributedSynchronizationProvider(ZooKeeperPath, string, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>) Constructor

Constructs a provider which uses [connectionString](ZooKeeperDistributedSynchronizationProvider..ctor.cwicK93j0K7ho3vCLOhbSw.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.ZooKeeperDistributedSynchronizationProvider(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).connectionString 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.ZooKeeperDistributedSynchronizationProvider(Medallion.Threading.ZooKeeper.ZooKeeperPath, string, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).connectionString') and [options](ZooKeeperDistributedSynchronizationProvider..ctor.cwicK93j0K7ho3vCLOhbSw.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.ZooKeeperDistributedSynchronizationProvider(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).options 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.ZooKeeperDistributedSynchronizationProvider(Medallion.Threading.ZooKeeper.ZooKeeperPath, string, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).options'). Lock and semaphore nodes will be created  
in [directoryPath](ZooKeeperDistributedSynchronizationProvider..ctor.cwicK93j0K7ho3vCLOhbSw.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.ZooKeeperDistributedSynchronizationProvider(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).directoryPath 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.ZooKeeperDistributedSynchronizationProvider(Medallion.Threading.ZooKeeper.ZooKeeperPath, string, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).directoryPath').

```csharp
public ZooKeeperDistributedSynchronizationProvider(Medallion.Threading.ZooKeeper.ZooKeeperPath directoryPath, string connectionString, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.ZooKeeperDistributedSynchronizationProvider(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).directoryPath'></a>

`directoryPath` [ZooKeeperPath](ZooKeeperPath.md 'Medallion.Threading.ZooKeeper.ZooKeeperPath')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.ZooKeeperDistributedSynchronizationProvider(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).connectionString'></a>

`connectionString` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.ZooKeeperDistributedSynchronizationProvider(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[ZooKeeperDistributedSynchronizationOptionsBuilder](ZooKeeperDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')