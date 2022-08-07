#### [DistributedLock.MySql](README.md 'README')
### [Medallion.Threading.MySql](Medallion.Threading.MySql.md 'Medallion.Threading.MySql')

## MySqlConnectionOptionsBuilder Class

Specifies options for connecting to and locking against a MySQL database

```csharp
public sealed class MySqlConnectionOptionsBuilder
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; MySqlConnectionOptionsBuilder

| Methods | |
| :--- | :--- |
| [KeepaliveCadence(TimeSpan)](MySqlConnectionOptionsBuilder.KeepaliveCadence.UQXLJo99rQgbid2s4ZOgRQ.md 'Medallion.Threading.MySql.MySqlConnectionOptionsBuilder.KeepaliveCadence(System.TimeSpan)') | MySQL's wait_timeout system variable determines how long the server will allow a connection to be idle before killing it.<br/>For more information, see https://dev.mysql.com/doc/refman/5.7/en/server-system-variables.html#sysvar_wait_timeout.<br/><br/>To prevent this, this option sets the cadence at which we run a no-op "keepalive" query on a connection that is holding a lock.<br/><br/>Because MySQL's default for this setting is 8 hours, the default [keepaliveCadence](MySqlConnectionOptionsBuilder.KeepaliveCadence.UQXLJo99rQgbid2s4ZOgRQ.md#Medallion.Threading.MySql.MySqlConnectionOptionsBuilder.KeepaliveCadence(System.TimeSpan).keepaliveCadence 'Medallion.Threading.MySql.MySqlConnectionOptionsBuilder.KeepaliveCadence(System.TimeSpan).keepaliveCadence') is 3.5 hours.<br/><br/>Setting a value of [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan') disables keepalive. |
| [UseMultiplexing(bool)](MySqlConnectionOptionsBuilder.UseMultiplexing.fKIzw1BfXbV8NLqGfoikqw.md 'Medallion.Threading.MySql.MySqlConnectionOptionsBuilder.UseMultiplexing(bool)') | This mode takes advantage of the fact that while "holding" a lock (or other synchronization primitive)<br/>a connection is essentially idle. Thus, rather than creating a new connection for each held lock it is <br/>often possible to multiplex a shared connection so that that connection can hold multiple locks at the same time.<br/><br/>Multiplexing is on by default.<br/><br/>This is implemented in such a way that releasing a lock held on such a connection will never be blocked by an<br/>Acquire() call that is waiting to acquire a lock on that same connection. For this reason, the multiplexing<br/>strategy is "optimistic": if the lock can't be acquired instantaneously on the shared connection, a new (shareable) <br/>connection will be allocated.<br/><br/>This option can improve performance and avoid connection pool starvation in high-load scenarios. It is also<br/>particularly applicable to cases where [TryAcquire(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLock.TryAcquire.GcM73KNvUAY5aoOOhgln1g.md 'Medallion.Threading.IDistributedLock.TryAcquire(System.TimeSpan,System.Threading.CancellationToken)')<br/>semantics are used with a zero-length timeout. |
