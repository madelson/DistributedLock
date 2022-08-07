#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis').[RedisDistributedLock](RedisDistributedLock.md 'Medallion.Threading.Redis.RedisDistributedLock')

## RedisDistributedLock(RedisKey, IEnumerable<IDatabase>, Action<RedisDistributedSynchronizationOptionsBuilder>) Constructor

Constructs a lock named [key](RedisDistributedLock..ctor.CAJif2Aj+id/ZDgt9bfHHg.md#Medallion.Threading.Redis.RedisDistributedLock.RedisDistributedLock(RedisKey,System.Collections.Generic.IEnumerable_IDatabase_,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).key 'Medallion.Threading.Redis.RedisDistributedLock.RedisDistributedLock(RedisKey, System.Collections.Generic.IEnumerable<IDatabase>, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).key') using the provided [databases](RedisDistributedLock..ctor.CAJif2Aj+id/ZDgt9bfHHg.md#Medallion.Threading.Redis.RedisDistributedLock.RedisDistributedLock(RedisKey,System.Collections.Generic.IEnumerable_IDatabase_,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).databases 'Medallion.Threading.Redis.RedisDistributedLock.RedisDistributedLock(RedisKey, System.Collections.Generic.IEnumerable<IDatabase>, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).databases') and [options](RedisDistributedLock..ctor.CAJif2Aj+id/ZDgt9bfHHg.md#Medallion.Threading.Redis.RedisDistributedLock.RedisDistributedLock(RedisKey,System.Collections.Generic.IEnumerable_IDatabase_,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).options 'Medallion.Threading.Redis.RedisDistributedLock.RedisDistributedLock(RedisKey, System.Collections.Generic.IEnumerable<IDatabase>, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).options').

```csharp
public RedisDistributedLock(RedisKey key, System.Collections.Generic.IEnumerable<IDatabase> databases, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.Redis.RedisDistributedLock.RedisDistributedLock(RedisKey,System.Collections.Generic.IEnumerable_IDatabase_,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).key'></a>

`key` [StackExchange.Redis.RedisKey](https://docs.microsoft.com/en-us/dotnet/api/StackExchange.Redis.RedisKey 'StackExchange.Redis.RedisKey')

<a name='Medallion.Threading.Redis.RedisDistributedLock.RedisDistributedLock(RedisKey,System.Collections.Generic.IEnumerable_IDatabase_,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).databases'></a>

`databases` [System.Collections.Generic.IEnumerable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IEnumerable-1 'System.Collections.Generic.IEnumerable`1')[StackExchange.Redis.IDatabase](https://docs.microsoft.com/en-us/dotnet/api/StackExchange.Redis.IDatabase 'StackExchange.Redis.IDatabase')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IEnumerable-1 'System.Collections.Generic.IEnumerable`1')

<a name='Medallion.Threading.Redis.RedisDistributedLock.RedisDistributedLock(RedisKey,System.Collections.Generic.IEnumerable_IDatabase_,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[RedisDistributedSynchronizationOptionsBuilder](RedisDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')