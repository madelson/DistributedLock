#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading').[IDistributedSynchronizationHandle](IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle')

## IDistributedSynchronizationHandle.HandleLostToken Property

Gets a [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken') instance which may be used to   
monitor whether the handle to the lock is lost before the handle is  
disposed.   
  
For example, this could happen if the lock is backed by a   
database and the connection to the database is disrupted.  
  
Not all lock types support this; those that don't will return [System.Threading.CancellationToken.None](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken.None 'System.Threading.CancellationToken.None')  
which can be detected by checking [System.Threading.CancellationToken.CanBeCanceled](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken.CanBeCanceled 'System.Threading.CancellationToken.CanBeCanceled').  
  
For lock types that do support this, accessing this property may incur additional  
costs, such as polling to detect connectivity loss.

```csharp
System.Threading.CancellationToken HandleLostToken { get; }
```

#### Property Value
[System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')