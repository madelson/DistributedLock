#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis').[RedisDistributedLock](RedisDistributedLock.md 'Medallion.Threading.Redis.RedisDistributedLock')

## RedisDistributedLock(RedisKey, IDatabase, Action<RedisDistributedSynchronizationOptionsBuilder>) Constructor

Constructs a lock named [key](RedisDistributedLock..ctor.umu5PN9uhldmmDNU7XvM2g.md#Medallion.Threading.Redis.RedisDistributedLock.RedisDistributedLock(RedisKey,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).key 'Medallion.Threading.Redis.RedisDistributedLock.RedisDistributedLock(RedisKey, IDatabase, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).key') using the provided [database](RedisDistributedLock..ctor.umu5PN9uhldmmDNU7XvM2g.md#Medallion.Threading.Redis.RedisDistributedLock.RedisDistributedLock(RedisKey,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).database 'Medallion.Threading.Redis.RedisDistributedLock.RedisDistributedLock(RedisKey, IDatabase, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).database') and [options](RedisDistributedLock..ctor.umu5PN9uhldmmDNU7XvM2g.md#Medallion.Threading.Redis.RedisDistributedLock.RedisDistributedLock(RedisKey,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).options 'Medallion.Threading.Redis.RedisDistributedLock.RedisDistributedLock(RedisKey, IDatabase, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).options').

```csharp
public RedisDistributedLock(RedisKey key, IDatabase database, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.Redis.RedisDistributedLock.RedisDistributedLock(RedisKey,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).key'></a>

`key` [StackExchange.Redis.RedisKey](https://docs.microsoft.com/en-us/dotnet/api/StackExchange.Redis.RedisKey 'StackExchange.Redis.RedisKey')

<a name='Medallion.Threading.Redis.RedisDistributedLock.RedisDistributedLock(RedisKey,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).database'></a>

`database` [StackExchange.Redis.IDatabase](https://docs.microsoft.com/en-us/dotnet/api/StackExchange.Redis.IDatabase 'StackExchange.Redis.IDatabase')

<a name='Medallion.Threading.Redis.RedisDistributedLock.RedisDistributedLock(RedisKey,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[RedisDistributedSynchronizationOptionsBuilder](RedisDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')