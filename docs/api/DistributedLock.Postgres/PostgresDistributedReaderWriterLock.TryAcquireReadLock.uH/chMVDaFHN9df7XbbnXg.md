#### [DistributedLock.Postgres](README.md 'README')
### [Medallion.Threading.Postgres](Medallion.Threading.Postgres.md 'Medallion.Threading.Postgres').[PostgresDistributedReaderWriterLock](PostgresDistributedReaderWriterLock.md 'Medallion.Threading.Postgres.PostgresDistributedReaderWriterLock')

## PostgresDistributedReaderWriterLock.TryAcquireReadLock(TimeSpan, CancellationToken) Method

Attempts to acquire a READ lock synchronously. Multiple readers are allowed. Not compatible with a WRITE lock. Usage:   
  
```csharp  
using (var handle = myLock.TryAcquireReadLock(...))  
{  
    if (handle != null) { /* we have the lock! */ }  
}  
// dispose releases the lock if we took it  
```

```csharp
public Medallion.Threading.Postgres.PostgresDistributedReaderWriterLockHandle? TryAcquireReadLock(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.Postgres.PostgresDistributedReaderWriterLock.TryAcquireReadLock(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.Postgres.PostgresDistributedReaderWriterLock.TryAcquireReadLock(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [TryAcquireReadLock(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedReaderWriterLock.TryAcquireReadLock.FwhFBAUmx9brWLKd6O1SSw.md 'Medallion.Threading.IDistributedReaderWriterLock.TryAcquireReadLock(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[PostgresDistributedReaderWriterLockHandle](PostgresDistributedReaderWriterLockHandle.md 'Medallion.Threading.Postgres.PostgresDistributedReaderWriterLockHandle')  
A [PostgresDistributedReaderWriterLockHandle](PostgresDistributedReaderWriterLockHandle.md 'Medallion.Threading.Postgres.PostgresDistributedReaderWriterLockHandle') which can be used to release the lock or null on failure