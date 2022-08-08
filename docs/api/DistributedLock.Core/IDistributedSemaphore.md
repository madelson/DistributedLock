#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading')

## IDistributedSemaphore Interface

A synchronization primitive which restricts access to a resource or critical section of code to a fixed number of concurrent threads/processes.
Compare to [System.Threading.Semaphore](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Semaphore 'System.Threading.Semaphore').

```csharp
public interface IDistributedSemaphore
```

| Properties | |
| :--- | :--- |
| [MaxCount](IDistributedSemaphore.MaxCount.md 'Medallion.Threading.IDistributedSemaphore.MaxCount') | The maximum number of "tickets" available for the semaphore (ie the number of processes which can acquire the semaphore concurrently). |
| [Name](IDistributedSemaphore.Name.md 'Medallion.Threading.IDistributedSemaphore.Name') | A name that uniquely identifies the semaphore |

| Methods | |
| :--- | :--- |
| [Acquire(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedSemaphore.Acquire.Idy1BAzgGUWQ22QmqRZDsg.md 'Medallion.Threading.IDistributedSemaphore.Acquire(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires a semaphore ticket synchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: |
| [AcquireAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedSemaphore.AcquireAsync.72hbd/OOOHUBoRAQHgD31Q.md 'Medallion.Threading.IDistributedSemaphore.AcquireAsync(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires a semaphore ticket asynchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: |
| [TryAcquire(TimeSpan, CancellationToken)](IDistributedSemaphore.TryAcquire.G9QqgKI96XBtpNQoUp0RZg.md 'Medallion.Threading.IDistributedSemaphore.TryAcquire(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire a semaphore ticket synchronously. Usage: |
| [TryAcquireAsync(TimeSpan, CancellationToken)](IDistributedSemaphore.TryAcquireAsync.yTpJMeiQTyO40ByV0nmdkQ.md 'Medallion.Threading.IDistributedSemaphore.TryAcquireAsync(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire a semaphore ticket asynchronously. Usage: |
