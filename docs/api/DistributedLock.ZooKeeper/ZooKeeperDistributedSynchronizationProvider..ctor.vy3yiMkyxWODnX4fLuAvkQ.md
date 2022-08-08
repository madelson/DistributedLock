#### [DistributedLock.ZooKeeper](README.md 'README')
### [Medallion.Threading.ZooKeeper](Medallion.Threading.ZooKeeper.md 'Medallion.Threading.ZooKeeper').[ZooKeeperDistributedSynchronizationProvider](ZooKeeperDistributedSynchronizationProvider.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider')

## ZooKeeperDistributedSynchronizationProvider(string, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>) Constructor

Constructs a provider which uses [connectionString](ZooKeeperDistributedSynchronizationProvider..ctor.vy3yiMkyxWODnX4fLuAvkQ.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.ZooKeeperDistributedSynchronizationProvider(string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).connectionString 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.ZooKeeperDistributedSynchronizationProvider(string, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).connectionString') and [options](ZooKeeperDistributedSynchronizationProvider..ctor.vy3yiMkyxWODnX4fLuAvkQ.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.ZooKeeperDistributedSynchronizationProvider(string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).options 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.ZooKeeperDistributedSynchronizationProvider(string, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).options'). Lock and semaphore nodes will be created
in the root directory '/'.

```csharp
public ZooKeeperDistributedSynchronizationProvider(string connectionString, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.ZooKeeperDistributedSynchronizationProvider(string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).connectionString'></a>

`connectionString` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationProvider.ZooKeeperDistributedSynchronizationProvider(string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[ZooKeeperDistributedSynchronizationOptionsBuilder](ZooKeeperDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')