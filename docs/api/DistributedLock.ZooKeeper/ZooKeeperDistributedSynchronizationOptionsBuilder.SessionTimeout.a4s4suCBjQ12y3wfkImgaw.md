#### [DistributedLock.ZooKeeper](README.md 'README')
### [Medallion.Threading.ZooKeeper](Medallion.Threading.ZooKeeper.md 'Medallion.Threading.ZooKeeper').[ZooKeeperDistributedSynchronizationOptionsBuilder](ZooKeeperDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder')

## ZooKeeperDistributedSynchronizationOptionsBuilder.SessionTimeout(TimeSpan) Method

Configures the [sessionTimeout](ZooKeeperDistributedSynchronizationOptionsBuilder.SessionTimeout.a4s4suCBjQ12y3wfkImgaw.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder.SessionTimeout(System.TimeSpan).sessionTimeout 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder.SessionTimeout(System.TimeSpan).sessionTimeout') for connections to ZooKeeper. Because the underlying ZooKeeper client periodically renews
the session, this value generally will not impact behavior. Lower values mean that locks will be released more quickly following a crash
of the lock-holding process, but also increase the risk that transient connection issues will result in a dropped lock.

Defaults to 20s.

```csharp
public Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder SessionTimeout(System.TimeSpan sessionTimeout);
```
#### Parameters

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder.SessionTimeout(System.TimeSpan).sessionTimeout'></a>

`sessionTimeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

#### Returns
[ZooKeeperDistributedSynchronizationOptionsBuilder](ZooKeeperDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder')