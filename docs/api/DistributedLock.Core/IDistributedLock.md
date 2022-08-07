#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading')

## IDistributedLock Interface

A mutex synchronization primitive which can be used to coordinate access to a resource or critical region of code  
across processes or systems. The scope and capabilities of the lock are dependent on the particular implementation

```csharp
public interface IDistributedLock
```

| Properties | |
| :--- | :--- |
| [Name](IDistributedLock.Name.md 'Medallion.Threading.IDistributedLock.Name') | A name that uniquely identifies the lock |

| Methods | |
| :--- | :--- |
| [Acquire(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedLock.Acquire.Q+8FXimBZqUrDv5tTRw59w.md 'Medallion.Threading.IDistributedLock.Acquire(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires the lock synchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: |
| [AcquireAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedLock.AcquireAsync.0Lol7Hv58Kl+UVYSOI6IpQ.md 'Medallion.Threading.IDistributedLock.AcquireAsync(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires the lock asynchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: |
| [TryAcquire(TimeSpan, CancellationToken)](IDistributedLock.TryAcquire.GcM73KNvUAY5aoOOhgln1g.md 'Medallion.Threading.IDistributedLock.TryAcquire(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire the lock synchronously. Usage: |
| [TryAcquireAsync(TimeSpan, CancellationToken)](IDistributedLock.TryAcquireAsync.ZLhweq3GadK5OwGmTwruEQ.md 'Medallion.Threading.IDistributedLock.TryAcquireAsync(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire the lock asynchronously. Usage: |
