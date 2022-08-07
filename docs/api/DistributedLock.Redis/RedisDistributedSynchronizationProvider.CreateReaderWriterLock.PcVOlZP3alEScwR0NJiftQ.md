#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis').[RedisDistributedSynchronizationProvider](RedisDistributedSynchronizationProvider.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationProvider')

## RedisDistributedSynchronizationProvider.CreateReaderWriterLock(string) Method

Creates a [RedisDistributedReaderWriterLock](RedisDistributedReaderWriterLock.md 'Medallion.Threading.Redis.RedisDistributedReaderWriterLock') using the given [name](RedisDistributedSynchronizationProvider.CreateReaderWriterLock.PcVOlZP3alEScwR0NJiftQ.md#Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.CreateReaderWriterLock(string).name 'Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.CreateReaderWriterLock(string).name').

```csharp
public Medallion.Threading.Redis.RedisDistributedReaderWriterLock CreateReaderWriterLock(string name);
```
#### Parameters

<a name='Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.CreateReaderWriterLock(string).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

Implements [CreateReaderWriterLock(string)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedReaderWriterLockProvider.CreateReaderWriterLock.BJyxJJllIyIqdlfqBHLDTA.md 'Medallion.Threading.IDistributedReaderWriterLockProvider.CreateReaderWriterLock(System.String)')

#### Returns
[RedisDistributedReaderWriterLock](RedisDistributedReaderWriterLock.md 'Medallion.Threading.Redis.RedisDistributedReaderWriterLock')