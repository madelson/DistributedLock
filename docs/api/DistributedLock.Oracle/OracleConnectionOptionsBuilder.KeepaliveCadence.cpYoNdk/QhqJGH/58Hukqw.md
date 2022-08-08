#### [DistributedLock.Oracle](README.md 'README')
### [Medallion.Threading.Oracle](Medallion.Threading.Oracle.md 'Medallion.Threading.Oracle').[OracleConnectionOptionsBuilder](OracleConnectionOptionsBuilder.md 'Medallion.Threading.Oracle.OracleConnectionOptionsBuilder')

## OracleConnectionOptionsBuilder.KeepaliveCadence(TimeSpan) Method

Oracle does not kill idle connections by default, so by default keepalive is disabled (set to [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan')).

However, if you are using the IDLE_TIME setting in Oracle or if your network is dropping connections that are idle holding locks for
a long time, you can set a value for keepalive to prevent this from happening.

See https://stackoverflow.com/questions/1966247/idle-timeout-parameter-in-oracle.

```csharp
public Medallion.Threading.Oracle.OracleConnectionOptionsBuilder KeepaliveCadence(System.TimeSpan keepaliveCadence);
```
#### Parameters

<a name='Medallion.Threading.Oracle.OracleConnectionOptionsBuilder.KeepaliveCadence(System.TimeSpan).keepaliveCadence'></a>

`keepaliveCadence` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

#### Returns
[OracleConnectionOptionsBuilder](OracleConnectionOptionsBuilder.md 'Medallion.Threading.Oracle.OracleConnectionOptionsBuilder')