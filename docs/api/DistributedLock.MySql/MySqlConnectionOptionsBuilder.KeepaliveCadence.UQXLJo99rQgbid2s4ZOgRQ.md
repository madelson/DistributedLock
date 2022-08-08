#### [DistributedLock.MySql](README.md 'README')
### [Medallion.Threading.MySql](Medallion.Threading.MySql.md 'Medallion.Threading.MySql').[MySqlConnectionOptionsBuilder](MySqlConnectionOptionsBuilder.md 'Medallion.Threading.MySql.MySqlConnectionOptionsBuilder')

## MySqlConnectionOptionsBuilder.KeepaliveCadence(TimeSpan) Method

MySQL's wait_timeout system variable determines how long the server will allow a connection to be idle before killing it.
For more information, see https://dev.mysql.com/doc/refman/5.7/en/server-system-variables.html#sysvar_wait_timeout.

To prevent this, this option sets the cadence at which we run a no-op "keepalive" query on a connection that is holding a lock.

Because MySQL's default for this setting is 8 hours, the default [keepaliveCadence](MySqlConnectionOptionsBuilder.KeepaliveCadence.UQXLJo99rQgbid2s4ZOgRQ.md#Medallion.Threading.MySql.MySqlConnectionOptionsBuilder.KeepaliveCadence(System.TimeSpan).keepaliveCadence 'Medallion.Threading.MySql.MySqlConnectionOptionsBuilder.KeepaliveCadence(System.TimeSpan).keepaliveCadence') is 3.5 hours.

Setting a value of [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan') disables keepalive.

```csharp
public Medallion.Threading.MySql.MySqlConnectionOptionsBuilder KeepaliveCadence(System.TimeSpan keepaliveCadence);
```
#### Parameters

<a name='Medallion.Threading.MySql.MySqlConnectionOptionsBuilder.KeepaliveCadence(System.TimeSpan).keepaliveCadence'></a>

`keepaliveCadence` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

#### Returns
[MySqlConnectionOptionsBuilder](MySqlConnectionOptionsBuilder.md 'Medallion.Threading.MySql.MySqlConnectionOptionsBuilder')