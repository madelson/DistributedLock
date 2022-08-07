#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlConnectionOptionsBuilder](SqlConnectionOptionsBuilder.md 'Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder')

## SqlConnectionOptionsBuilder.UseMultiplexing(bool) Method

This mode takes advantage of the fact that while "holding" a lock (or other synchronization primitive)  
a connection is essentially idle. Thus, rather than creating a new connection for each held lock it is   
often possible to multiplex a shared connection so that that connection can hold multiple locks at the same time.  
  
Multiplexing is on by default.  
  
This is implemented in such a way that releasing a lock held on such a connection will never be blocked by an  
Acquire() call that is waiting to acquire a lock on that same connection. For this reason, the multiplexing  
strategy is "optimistic": if the lock can't be acquired instantaneously on the shared connection, a new (shareable)   
connection will be allocated.  
  
This option can improve performance and avoid connection pool starvation in high-load scenarios. It is also  
particularly applicable to cases where [TryAcquire(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLock.TryAcquire.GcM73KNvUAY5aoOOhgln1g.md 'Medallion.Threading.IDistributedLock.TryAcquire(System.TimeSpan,System.Threading.CancellationToken)')  
semantics are used with a zero-length timeout.

```csharp
public Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder UseMultiplexing(bool useMultiplexing=true);
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder.UseMultiplexing(bool).useMultiplexing'></a>

`useMultiplexing` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')

#### Returns
[SqlConnectionOptionsBuilder](SqlConnectionOptionsBuilder.md 'Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder')