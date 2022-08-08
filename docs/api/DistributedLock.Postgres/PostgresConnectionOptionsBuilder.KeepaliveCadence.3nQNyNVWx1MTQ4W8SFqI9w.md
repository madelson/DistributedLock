#### [DistributedLock.Postgres](README.md 'README')
### [Medallion.Threading.Postgres](Medallion.Threading.Postgres.md 'Medallion.Threading.Postgres').[PostgresConnectionOptionsBuilder](PostgresConnectionOptionsBuilder.md 'Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder')

## PostgresConnectionOptionsBuilder.KeepaliveCadence(TimeSpan) Method

Some Postgres setups have automation in place which aggressively kills idle connections.

To prevent this, this option sets the cadence at which we run a no-op "keepalive" query on a connection that is holding a lock. 
Note that this still does not guarantee protection for the connection from all conditions where the governor might kill it.

Defaults to [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan'), which disables keepalive.

```csharp
public Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder KeepaliveCadence(System.TimeSpan keepaliveCadence);
```
#### Parameters

<a name='Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder.KeepaliveCadence(System.TimeSpan).keepaliveCadence'></a>

`keepaliveCadence` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

#### Returns
[PostgresConnectionOptionsBuilder](PostgresConnectionOptionsBuilder.md 'Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder')