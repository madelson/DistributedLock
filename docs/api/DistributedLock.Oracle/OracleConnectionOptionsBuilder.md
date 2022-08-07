#### [DistributedLock.Oracle](README.md 'README')
### [Medallion.Threading.Oracle](Medallion.Threading.Oracle.md 'Medallion.Threading.Oracle')

## OracleConnectionOptionsBuilder Class

Specifies options for connecting to and locking against an Oracle database

```csharp
public sealed class OracleConnectionOptionsBuilder
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; OracleConnectionOptionsBuilder

| Methods | |
| :--- | :--- |
| [KeepaliveCadence(TimeSpan)](OracleConnectionOptionsBuilder.KeepaliveCadence.cpYoNdk/QhqJGH/58Hukqw.md 'Medallion.Threading.Oracle.OracleConnectionOptionsBuilder.KeepaliveCadence(System.TimeSpan)') | Oracle does not kill idle connections by default, so by default keepalive is disabled (set to [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan')).<br/><br/>However, if you are using the IDLE_TIME setting in Oracle or if your network is dropping connections that are idle holding locks for<br/>a long time, you can set a value for keepalive to prevent this from happening.<br/><br/>See https://stackoverflow.com/questions/1966247/idle-timeout-parameter-in-oracle. |
| [UseMultiplexing(bool)](OracleConnectionOptionsBuilder.UseMultiplexing.A+iIKatFl3VDvz/7Q6X3Jg.md 'Medallion.Threading.Oracle.OracleConnectionOptionsBuilder.UseMultiplexing(bool)') | This mode takes advantage of the fact that while "holding" a lock (or other synchronization primitive)<br/>a connection is essentially idle. Thus, rather than creating a new connection for each held lock it is <br/>often possible to multiplex a shared connection so that that connection can hold multiple locks at the same time.<br/><br/>Multiplexing is on by default.<br/><br/>This is implemented in such a way that releasing a lock held on such a connection will never be blocked by an<br/>Acquire() call that is waiting to acquire a lock on that same connection. For this reason, the multiplexing<br/>strategy is "optimistic": if the lock can't be acquired instantaneously on the shared connection, a new (shareable) <br/>connection will be allocated.<br/><br/>This option can improve performance and avoid connection pool starvation in high-load scenarios. It is also<br/>particularly applicable to cases where [TryAcquire(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLock.TryAcquire.GcM73KNvUAY5aoOOhgln1g.md 'Medallion.Threading.IDistributedLock.TryAcquire(System.TimeSpan,System.Threading.CancellationToken)')<br/>semantics are used with a zero-length timeout. |
