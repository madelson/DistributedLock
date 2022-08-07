#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis').[RedisDistributedSynchronizationProvider](RedisDistributedSynchronizationProvider.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationProvider')

## RedisDistributedSynchronizationProvider.CreateLock(RedisKey) Method

Creates a [RedisDistributedLock](RedisDistributedLock.md 'Medallion.Threading.Redis.RedisDistributedLock') using the given [key](RedisDistributedSynchronizationProvider.CreateLock.Po1Hx+wws4GErhhVofAcYw.md#Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.CreateLock(RedisKey).key 'Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.CreateLock(RedisKey).key').

```csharp
public Medallion.Threading.Redis.RedisDistributedLock CreateLock(RedisKey key);
```
#### Parameters

<a name='Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.CreateLock(RedisKey).key'></a>

`key` [StackExchange.Redis.RedisKey](https://docs.microsoft.com/en-us/dotnet/api/StackExchange.Redis.RedisKey 'StackExchange.Redis.RedisKey')

#### Returns
[RedisDistributedLock](RedisDistributedLock.md 'Medallion.Threading.Redis.RedisDistributedLock')