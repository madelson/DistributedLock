#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis').[RedisDistributedSemaphore](RedisDistributedSemaphore.md 'Medallion.Threading.Redis.RedisDistributedSemaphore')

## RedisDistributedSemaphore(RedisKey, int, IDatabase, Action<RedisDistributedSynchronizationOptionsBuilder>) Constructor

Constructs a semaphore named [key](RedisDistributedSemaphore..ctor.uV7HEVNcvSXDYGL/CEuDZg.md#Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey,int,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).key 'Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey, int, IDatabase, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).key') using the provided [maxCount](RedisDistributedSemaphore..ctor.uV7HEVNcvSXDYGL/CEuDZg.md#Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey,int,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).maxCount 'Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey, int, IDatabase, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).maxCount'), [database](RedisDistributedSemaphore..ctor.uV7HEVNcvSXDYGL/CEuDZg.md#Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey,int,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).database 'Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey, int, IDatabase, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).database'), and [options](RedisDistributedSemaphore..ctor.uV7HEVNcvSXDYGL/CEuDZg.md#Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey,int,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).options 'Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey, int, IDatabase, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).options').

```csharp
public RedisDistributedSemaphore(RedisKey key, int maxCount, IDatabase database, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey,int,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).key'></a>

`key` [StackExchange.Redis.RedisKey](https://docs.microsoft.com/en-us/dotnet/api/StackExchange.Redis.RedisKey 'StackExchange.Redis.RedisKey')

<a name='Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey,int,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).maxCount'></a>

`maxCount` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

<a name='Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey,int,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).database'></a>

`database` [StackExchange.Redis.IDatabase](https://docs.microsoft.com/en-us/dotnet/api/StackExchange.Redis.IDatabase 'StackExchange.Redis.IDatabase')

<a name='Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey,int,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[RedisDistributedSynchronizationOptionsBuilder](RedisDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')