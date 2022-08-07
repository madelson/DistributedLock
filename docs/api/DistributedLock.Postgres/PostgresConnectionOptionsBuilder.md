#### [DistributedLock.Postgres](README.md 'README')
### [Medallion.Threading.Postgres](Medallion.Threading.Postgres.md 'Medallion.Threading.Postgres')

## PostgresConnectionOptionsBuilder Class

Specifies options for connecting to and locking against a Postgres database

```csharp
public sealed class PostgresConnectionOptionsBuilder
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; PostgresConnectionOptionsBuilder

| Methods | |
| :--- | :--- |
| [KeepaliveCadence(TimeSpan)](PostgresConnectionOptionsBuilder.KeepaliveCadence.3nQNyNVWx1MTQ4W8SFqI9w.md 'Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder.KeepaliveCadence(System.TimeSpan)') | Some Postgres setups have automation in place which aggressively kills idle connections.<br/><br/>To prevent this, this option sets the cadence at which we run a no-op "keepalive" query on a connection that is holding a lock. <br/>Note that this still does not guarantee protection for the connection from all conditions where the governor might kill it.<br/><br/>Defaults to [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan'), which disables keepalive. |
| [UseMultiplexing(bool)](PostgresConnectionOptionsBuilder.UseMultiplexing.iaWbIbzQ+9I/FdhQ+S89ng.md 'Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder.UseMultiplexing(bool)') | This mode takes advantage of the fact that while "holding" a lock (or other synchronization primitive)<br/>a connection is essentially idle. Thus, rather than creating a new connection for each held lock it is <br/>often possible to multiplex a shared connection so that that connection can hold multiple locks at the same time.<br/><br/>Multiplexing is on by default.<br/><br/>This is implemented in such a way that releasing a lock held on such a connection will never be blocked by an<br/>Acquire() call that is waiting to acquire a lock on that same connection. For this reason, the multiplexing<br/>strategy is "optimistic": if the lock can't be acquired instantaneously on the shared connection, a new (shareable) <br/>connection will be allocated.<br/><br/>This option can improve performance and avoid connection pool starvation in high-load scenarios. It is also<br/>particularly applicable to cases where [TryAcquire(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLock.TryAcquire.GcM73KNvUAY5aoOOhgln1g.md 'Medallion.Threading.IDistributedLock.TryAcquire(System.TimeSpan,System.Threading.CancellationToken)')<br/>semantics are used with a zero-length timeout. |
