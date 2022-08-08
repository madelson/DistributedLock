#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading')

## IDistributedSynchronizationHandle Interface

A handle to a distributed lock or other synchronization primitive. To unlock/release,
simply dispose the handle.

```csharp
public interface IDistributedSynchronizationHandle :
System.IDisposable,
System.IAsyncDisposable
```

Derived  
&#8627; [IDistributedLockUpgradeableHandle](IDistributedLockUpgradeableHandle.md 'Medallion.Threading.IDistributedLockUpgradeableHandle')

Implements [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable'), [System.IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IAsyncDisposable 'System.IAsyncDisposable')

| Properties | |
| :--- | :--- |
| [HandleLostToken](IDistributedSynchronizationHandle.HandleLostToken.md 'Medallion.Threading.IDistributedSynchronizationHandle.HandleLostToken') | Gets a [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken') instance which may be used to  monitor whether the handle to the lock is lost before the handle is disposed.   For example, this could happen if the lock is backed by a  database and the connection to the database is disrupted.  Not all lock types support this; those that don't will return [System.Threading.CancellationToken.None](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken.None 'System.Threading.CancellationToken.None') which can be detected by checking [System.Threading.CancellationToken.CanBeCanceled](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken.CanBeCanceled 'System.Threading.CancellationToken.CanBeCanceled').  For lock types that do support this, accessing this property may incur additional costs, such as polling to detect connectivity loss. |
