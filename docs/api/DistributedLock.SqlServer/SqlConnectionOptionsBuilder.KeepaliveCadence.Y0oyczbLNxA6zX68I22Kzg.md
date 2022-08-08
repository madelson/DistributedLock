#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlConnectionOptionsBuilder](SqlConnectionOptionsBuilder.md 'Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder')

## SqlConnectionOptionsBuilder.KeepaliveCadence(TimeSpan) Method

Using SQL Azure as a distributed synchronization provider can be challenging due to Azure's aggressive connection governor
which proactively kills idle connections. 

To prevent this, this option sets the cadence at which we run a no-op "keepalive" query on a connection that is holding a lock. 
Note that this still does not guarantee protection for the connection from all conditions where the governor might kill it.

To disable keepalive, set to [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan').

Defaults to 10 minutes based on Azure's 30 minute default behavior.

For more information, see the dicussion on https://github.com/madelson/DistributedLock/issues/5

```csharp
public Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder KeepaliveCadence(System.TimeSpan keepaliveCadence);
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder.KeepaliveCadence(System.TimeSpan).keepaliveCadence'></a>

`keepaliveCadence` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

#### Returns
[SqlConnectionOptionsBuilder](SqlConnectionOptionsBuilder.md 'Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder')