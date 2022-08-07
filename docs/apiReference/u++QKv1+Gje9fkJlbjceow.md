### [Medallion.Threading.WaitHandles](0cv6wmZCIva5FK3cOR8t5g.md 'Medallion.Threading.WaitHandles')

## WaitHandleDistributedSemaphore Class

Implements a distributed semaphore based on a global [System.Threading.Semaphore](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Semaphore 'System.Threading.Semaphore')

```csharp
public sealed class WaitHandleDistributedSemaphore :
Medallion.Threading.IDistributedSemaphore
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; WaitHandleDistributedSemaphore

Implements [Medallion.Threading.IDistributedSemaphore](https://docs.microsoft.com/en-us/dotnet/api/Medallion.Threading.IDistributedSemaphore 'Medallion.Threading.IDistributedSemaphore')

| Constructors | |
| :--- | :--- |
| [WaitHandleDistributedSemaphore(string, int, Nullable&lt;TimeSpan&gt;, bool)](4ysEtsHFYQ8UNvnVKspCjg.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string, int, System.Nullable<System.TimeSpan>, bool)') | Constructs a lock with the given [name](4ysEtsHFYQ8UNvnVKspCjg.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string,int,System.Nullable_System.TimeSpan_,bool).name 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string, int, System.Nullable<System.TimeSpan>, bool).name').<br/><br/>[abandonmentCheckCadence](4ysEtsHFYQ8UNvnVKspCjg.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string,int,System.Nullable_System.TimeSpan_,bool).abandonmentCheckCadence 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string, int, System.Nullable<System.TimeSpan>, bool).abandonmentCheckCadence') specifies how frequently we refresh our [System.Threading.Semaphore](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Semaphore 'System.Threading.Semaphore') object in case it is abandoned by<br/>its original owner. The default is 2s.<br/><br/>Unless [exactName](4ysEtsHFYQ8UNvnVKspCjg.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string,int,System.Nullable_System.TimeSpan_,bool).exactName 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string, int, System.Nullable<System.TimeSpan>, bool).exactName') is specified, [name](4ysEtsHFYQ8UNvnVKspCjg.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string,int,System.Nullable_System.TimeSpan_,bool).name 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string, int, System.Nullable<System.TimeSpan>, bool).name') will be escaped/hashed to ensure name validity. |

| Properties | |
| :--- | :--- |
| [MaxCount](_eKAd9KmIbGvBAAhA9RhEw.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.MaxCount') | Implements [Medallion.Threading.IDistributedSemaphore.MaxCount](https://docs.microsoft.com/en-us/dotnet/api/Medallion.Threading.IDistributedSemaphore.MaxCount 'Medallion.Threading.IDistributedSemaphore.MaxCount') |
| [Name](DrkUdCA1HTHqv41YDxrv5w.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.Name') | Implements [Medallion.Threading.IDistributedSemaphore.Name](https://docs.microsoft.com/en-us/dotnet/api/Medallion.Threading.IDistributedSemaphore.Name 'Medallion.Threading.IDistributedSemaphore.Name') |

| Methods | |
| :--- | :--- |
| [Acquire(Nullable&lt;TimeSpan&gt;, CancellationToken)](fanbWVO3U0ELSV89GyikgQ.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.Acquire(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires a semaphore ticket synchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: |
| [AcquireAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](k9Mv6k_1m4DGGHNWVI0rSw.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.AcquireAsync(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires a semaphore ticket asynchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: |
| [TryAcquire(TimeSpan, CancellationToken)](xAXKRX1HdsJVylWBC_+WmA.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.TryAcquire(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire a semaphore ticket synchronously. Usage: |
| [TryAcquireAsync(TimeSpan, CancellationToken)](nk6f9CuMtAPqAG7F0UWvSA.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.TryAcquireAsync(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire a semaphore ticket asynchronously. Usage: |
