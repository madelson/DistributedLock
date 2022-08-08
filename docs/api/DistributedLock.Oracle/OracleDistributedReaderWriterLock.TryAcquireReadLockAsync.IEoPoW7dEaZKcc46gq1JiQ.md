#### [DistributedLock.Oracle](README.md 'README')
### [Medallion.Threading.Oracle](Medallion.Threading.Oracle.md 'Medallion.Threading.Oracle').[OracleDistributedReaderWriterLock](OracleDistributedReaderWriterLock.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLock')

## OracleDistributedReaderWriterLock.TryAcquireReadLockAsync(TimeSpan, CancellationToken) Method

Attempts to acquire a READ lock asynchronously. Multiple readers are allowed. Not compatible with a WRITE lock. Usage: 

```csharp
await using (var handle = await myLock.TryAcquireReadLockAsync(...))
{
    if (handle != null) { /* we have the lock! */ }
}
// dispose releases the lock if we took it
```

```csharp
public System.Threading.Tasks.ValueTask<Medallion.Threading.Oracle.OracleDistributedReaderWriterLockHandle?> TryAcquireReadLockAsync(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.TryAcquireReadLockAsync(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.TryAcquireReadLockAsync(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [TryAcquireReadLockAsync(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedReaderWriterLock.TryAcquireReadLockAsync.1wx2S+CeVe62/fKwnr3rNQ.md 'Medallion.Threading.IDistributedReaderWriterLock.TryAcquireReadLockAsync(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[OracleDistributedReaderWriterLockHandle](OracleDistributedReaderWriterLockHandle.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockHandle')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')  
An [OracleDistributedReaderWriterLockHandle](OracleDistributedReaderWriterLockHandle.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockHandle') which can be used to release the lock or null on failure