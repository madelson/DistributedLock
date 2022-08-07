#### [DistributedLock.ZooKeeper](README.md 'README')
### [Medallion.Threading.ZooKeeper](Medallion.Threading.ZooKeeper.md 'Medallion.Threading.ZooKeeper').[ZooKeeperDistributedSynchronizationOptionsBuilder](ZooKeeperDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder')

## ZooKeeperDistributedSynchronizationOptionsBuilder.ConnectTimeout(TimeSpan) Method

Configures how long to wait to establish a connection to ZooKeeper before failing with a [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException').  
  
Defaults to 15s.

```csharp
public Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder ConnectTimeout(System.TimeSpan connectTimeout);
```
#### Parameters

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder.ConnectTimeout(System.TimeSpan).connectTimeout'></a>

`connectTimeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

#### Returns
[ZooKeeperDistributedSynchronizationOptionsBuilder](ZooKeeperDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder')