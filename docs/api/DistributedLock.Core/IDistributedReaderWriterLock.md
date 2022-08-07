#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading')

## IDistributedReaderWriterLock Interface

Provides distributed locking functionality comparable to [System.Threading.ReaderWriterLock](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.ReaderWriterLock 'System.Threading.ReaderWriterLock')

```csharp
public interface IDistributedReaderWriterLock
```

Derived  
&#8627; [IDistributedUpgradeableReaderWriterLock](IDistributedUpgradeableReaderWriterLock.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLock')

| Properties | |
| :--- | :--- |
| [Name](IDistributedReaderWriterLock.Name.md 'Medallion.Threading.IDistributedReaderWriterLock.Name') | A name that uniquely identifies the lock |

| Methods | |
| :--- | :--- |
| [AcquireReadLock(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedReaderWriterLock.AcquireReadLock.bAhgltfPpI+hi4bNotiyGg.md 'Medallion.Threading.IDistributedReaderWriterLock.AcquireReadLock(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires a READ lock synchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Multiple readers are allowed. Not compatible with a WRITE lock. Usage: |
| [AcquireReadLockAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedReaderWriterLock.AcquireReadLockAsync.otuQSEhQpAEqEEBnyFJHtw.md 'Medallion.Threading.IDistributedReaderWriterLock.AcquireReadLockAsync(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires a READ lock asynchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Multiple readers are allowed. Not compatible with a WRITE lock. Usage: |
| [AcquireWriteLock(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedReaderWriterLock.AcquireWriteLock.8zDirrYDrgr0WzrsnH7blA.md 'Medallion.Threading.IDistributedReaderWriterLock.AcquireWriteLock(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires a WRITE lock synchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Not compatible with another WRITE lock or an UPGRADE lock. Usage: |
| [AcquireWriteLockAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedReaderWriterLock.AcquireWriteLockAsync.fFyCc0HswQXUnGvjzNHJ+A.md 'Medallion.Threading.IDistributedReaderWriterLock.AcquireWriteLockAsync(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires a WRITE lock asynchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Not compatible with another WRITE lock or an UPGRADE lock. Usage: |
| [TryAcquireReadLock(TimeSpan, CancellationToken)](IDistributedReaderWriterLock.TryAcquireReadLock.FwhFBAUmx9brWLKd6O1SSw.md 'Medallion.Threading.IDistributedReaderWriterLock.TryAcquireReadLock(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire a READ lock synchronously. Multiple readers are allowed. Not compatible with a WRITE lock. Usage: |
| [TryAcquireReadLockAsync(TimeSpan, CancellationToken)](IDistributedReaderWriterLock.TryAcquireReadLockAsync.1wx2S+CeVe62/fKwnr3rNQ.md 'Medallion.Threading.IDistributedReaderWriterLock.TryAcquireReadLockAsync(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire a READ lock asynchronously. Multiple readers are allowed. Not compatible with a WRITE lock. Usage: |
| [TryAcquireWriteLock(TimeSpan, CancellationToken)](IDistributedReaderWriterLock.TryAcquireWriteLock.ypAYPzEP3B1U6LcOEQzWBw.md 'Medallion.Threading.IDistributedReaderWriterLock.TryAcquireWriteLock(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire a WRITE lock synchronously. Not compatible with another WRITE lock or an UPGRADE lock. Usage: |
| [TryAcquireWriteLockAsync(TimeSpan, CancellationToken)](IDistributedReaderWriterLock.TryAcquireWriteLockAsync.yhTsitSwERpacPdxWmUvww.md 'Medallion.Threading.IDistributedReaderWriterLock.TryAcquireWriteLockAsync(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire a WRITE lock asynchronously. Not compatible with another WRITE lock or an UPGRADE lock. Usage: |
