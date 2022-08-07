### [Medallion.Threading.WaitHandles](0cv6wmZCIva5FK3cOR8t5g.md 'Medallion.Threading.WaitHandles')

## EventWaitHandleDistributedLock Class

A distributed lock based on a global [System.Threading.EventWaitHandle](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.EventWaitHandle 'System.Threading.EventWaitHandle') on Windows.

```csharp
public sealed class EventWaitHandleDistributedLock :
Medallion.Threading.IDistributedLock
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; EventWaitHandleDistributedLock

Implements [Medallion.Threading.IDistributedLock](https://docs.microsoft.com/en-us/dotnet/api/Medallion.Threading.IDistributedLock 'Medallion.Threading.IDistributedLock')

| Constructors | |
| :--- | :--- |
| [EventWaitHandleDistributedLock(string, Nullable&lt;TimeSpan&gt;, bool)](2Tva732RJcbYY7yOGc2Dtg.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string, System.Nullable<System.TimeSpan>, bool)') | Constructs a lock with the given [name](2Tva732RJcbYY7yOGc2Dtg.md#Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string,System.Nullable_System.TimeSpan_,bool).name 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string, System.Nullable<System.TimeSpan>, bool).name').<br/><br/>[abandonmentCheckCadence](2Tva732RJcbYY7yOGc2Dtg.md#Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string,System.Nullable_System.TimeSpan_,bool).abandonmentCheckCadence 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string, System.Nullable<System.TimeSpan>, bool).abandonmentCheckCadence') specifies how frequently we refresh our [System.Threading.EventWaitHandle](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.EventWaitHandle 'System.Threading.EventWaitHandle') object in case it is abandoned by<br/>its original owner. The default is 2s.<br/><br/>Unless [exactName](2Tva732RJcbYY7yOGc2Dtg.md#Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string,System.Nullable_System.TimeSpan_,bool).exactName 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string, System.Nullable<System.TimeSpan>, bool).exactName') is specified, [name](2Tva732RJcbYY7yOGc2Dtg.md#Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string,System.Nullable_System.TimeSpan_,bool).name 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string, System.Nullable<System.TimeSpan>, bool).name') will be escaped/hashed to ensure name validity. |

| Properties | |
| :--- | :--- |
| [Name](0dCx0_H_hZmWHCpWnBDJvg.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.Name') | Implements [Medallion.Threading.IDistributedLock.Name](https://docs.microsoft.com/en-us/dotnet/api/Medallion.Threading.IDistributedLock.Name 'Medallion.Threading.IDistributedLock.Name') |

| Methods | |
| :--- | :--- |
| [Acquire(Nullable&lt;TimeSpan&gt;, CancellationToken)](csc50wjfYFMfhVMh9f0p5g.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.Acquire(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires the lock synchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: |
| [AcquireAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](Q_XRVgkI_77ehHSwgz3+6g.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.AcquireAsync(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires the lock asynchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: |
| [TryAcquire(TimeSpan, CancellationToken)](uJMovE7EIAyUhcQlWkoiMg.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.TryAcquire(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire the lock synchronously. Usage: |
| [TryAcquireAsync(TimeSpan, CancellationToken)](bF7Wpky_IL0puVDTMR7U2w.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.TryAcquireAsync(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire the lock asynchronously. Usage: |
