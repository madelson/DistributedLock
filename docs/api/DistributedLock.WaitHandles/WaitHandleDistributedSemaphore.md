#### [DistributedLock.WaitHandles](README.md 'README')
### [Medallion.Threading.WaitHandles](Medallion.Threading.WaitHandles.md 'Medallion.Threading.WaitHandles')

## WaitHandleDistributedSemaphore Class

Implements a distributed semaphore based on a global [System.Threading.Semaphore](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Semaphore 'System.Threading.Semaphore')

```csharp
public sealed class WaitHandleDistributedSemaphore :
Medallion.Threading.IDistributedSemaphore
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; WaitHandleDistributedSemaphore

Implements [IDistributedSemaphore](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSemaphore.md 'Medallion.Threading.IDistributedSemaphore')

| Constructors | |
| :--- | :--- |
| [WaitHandleDistributedSemaphore(string, int, Nullable&lt;TimeSpan&gt;, bool)](WaitHandleDistributedSemaphore..ctor.4ysEtsHFYQ8UNvnVKspCjg.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string, int, System.Nullable<System.TimeSpan>, bool)') | Constructs a lock with the given [name](WaitHandleDistributedSemaphore..ctor.4ysEtsHFYQ8UNvnVKspCjg.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string,int,System.Nullable_System.TimeSpan_,bool).name 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string, int, System.Nullable<System.TimeSpan>, bool).name').  [abandonmentCheckCadence](WaitHandleDistributedSemaphore..ctor.4ysEtsHFYQ8UNvnVKspCjg.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string,int,System.Nullable_System.TimeSpan_,bool).abandonmentCheckCadence 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string, int, System.Nullable<System.TimeSpan>, bool).abandonmentCheckCadence') specifies how frequently we refresh our [System.Threading.Semaphore](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Semaphore 'System.Threading.Semaphore') object in case it is abandoned by its original owner. The default is 2s.  Unless [exactName](WaitHandleDistributedSemaphore..ctor.4ysEtsHFYQ8UNvnVKspCjg.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string,int,System.Nullable_System.TimeSpan_,bool).exactName 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string, int, System.Nullable<System.TimeSpan>, bool).exactName') is specified, [name](WaitHandleDistributedSemaphore..ctor.4ysEtsHFYQ8UNvnVKspCjg.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string,int,System.Nullable_System.TimeSpan_,bool).name 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string, int, System.Nullable<System.TimeSpan>, bool).name') will be escaped/hashed to ensure name validity. |

| Properties | |
| :--- | :--- |
| [MaxCount](WaitHandleDistributedSemaphore.MaxCount.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.MaxCount') | Implements [MaxCount](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSemaphore.MaxCount.md 'Medallion.Threading.IDistributedSemaphore.MaxCount') |
| [Name](WaitHandleDistributedSemaphore.Name.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.Name') | Implements [Name](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSemaphore.Name.md 'Medallion.Threading.IDistributedSemaphore.Name') |

| Methods | |
| :--- | :--- |
| [Acquire(Nullable&lt;TimeSpan&gt;, CancellationToken)](WaitHandleDistributedSemaphore.Acquire.fanbWVO3U0ELSV89GyikgQ.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.Acquire(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires a semaphore ticket synchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: |
| [AcquireAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](WaitHandleDistributedSemaphore.AcquireAsync.k9Mv6k/1m4DGGHNWVI0rSw.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.AcquireAsync(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires a semaphore ticket asynchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: |
| [TryAcquire(TimeSpan, CancellationToken)](WaitHandleDistributedSemaphore.TryAcquire.xAXKRX1HdsJVylWBC/+WmA.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.TryAcquire(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire a semaphore ticket synchronously. Usage: |
| [TryAcquireAsync(TimeSpan, CancellationToken)](WaitHandleDistributedSemaphore.TryAcquireAsync.nk6f9CuMtAPqAG7F0UWvSA.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.TryAcquireAsync(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire a semaphore ticket asynchronously. Usage: |
