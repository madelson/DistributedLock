#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading')

## IDistributedUpgradeableReaderWriterLock Interface

Extends [IDistributedReaderWriterLock](IDistributedReaderWriterLock.md 'Medallion.Threading.IDistributedReaderWriterLock') with the ability to take an "upgrade" lock. Like a read lock, an upgrade lock   
allows for other concurrent read locks, but not for other upgrade or write locks. However, an upgrade lock can also be upgraded to a write  
lock without releasing the underlying handle.

```csharp
public interface IDistributedUpgradeableReaderWriterLock :
Medallion.Threading.IDistributedReaderWriterLock
```

Implements [IDistributedReaderWriterLock](IDistributedReaderWriterLock.md 'Medallion.Threading.IDistributedReaderWriterLock')

| Methods | |
| :--- | :--- |
| [AcquireUpgradeableReadLock(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedUpgradeableReaderWriterLock.AcquireUpgradeableReadLock.MgsbqNeNv0qen0RVQV8MHA.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLock.AcquireUpgradeableReadLock(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires an UPGRADE lock synchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Not compatible with another UPGRADE lock or a WRITE lock. Usage: |
| [AcquireUpgradeableReadLockAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedUpgradeableReaderWriterLock.AcquireUpgradeableReadLockAsync.XDD/LbfIJrScMNU14D5OvA.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLock.AcquireUpgradeableReadLockAsync(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires an UPGRADE lock asynchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Not compatible with another UPGRADE lock or a WRITE lock. Usage: |
| [TryAcquireUpgradeableReadLock(TimeSpan, CancellationToken)](IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLock.NcomTiK4v4VsrD5p8zrY6A.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLock(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire an UPGRADE lock synchronously. Not compatible with another UPGRADE lock or a WRITE lock. Usage: |
| [TryAcquireUpgradeableReadLockAsync(TimeSpan, CancellationToken)](IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLockAsync.NeQQ4jMkCO0IteXQSJv/1w.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLockAsync(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire an UPGRADE lock asynchronously. Not compatible with another UPGRADE lock or a WRITE lock. Usage: |
