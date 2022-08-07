#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis').[RedisDistributedReaderWriterLock](RedisDistributedReaderWriterLock.md 'Medallion.Threading.Redis.RedisDistributedReaderWriterLock')

## RedisDistributedReaderWriterLock(string, IEnumerable<IDatabase>, Action<RedisDistributedSynchronizationOptionsBuilder>) Constructor

Constructs a lock named [name](RedisDistributedReaderWriterLock..ctor.qFVv5NEfnVwOxiU3/Na6Nw.md#Medallion.Threading.Redis.RedisDistributedReaderWriterLock.RedisDistributedReaderWriterLock(string,System.Collections.Generic.IEnumerable_IDatabase_,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).name 'Medallion.Threading.Redis.RedisDistributedReaderWriterLock.RedisDistributedReaderWriterLock(string, System.Collections.Generic.IEnumerable<IDatabase>, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).name') using the provided [databases](RedisDistributedReaderWriterLock..ctor.qFVv5NEfnVwOxiU3/Na6Nw.md#Medallion.Threading.Redis.RedisDistributedReaderWriterLock.RedisDistributedReaderWriterLock(string,System.Collections.Generic.IEnumerable_IDatabase_,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).databases 'Medallion.Threading.Redis.RedisDistributedReaderWriterLock.RedisDistributedReaderWriterLock(string, System.Collections.Generic.IEnumerable<IDatabase>, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).databases') and [options](RedisDistributedReaderWriterLock..ctor.qFVv5NEfnVwOxiU3/Na6Nw.md#Medallion.Threading.Redis.RedisDistributedReaderWriterLock.RedisDistributedReaderWriterLock(string,System.Collections.Generic.IEnumerable_IDatabase_,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).options 'Medallion.Threading.Redis.RedisDistributedReaderWriterLock.RedisDistributedReaderWriterLock(string, System.Collections.Generic.IEnumerable<IDatabase>, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).options').

```csharp
public RedisDistributedReaderWriterLock(string name, System.Collections.Generic.IEnumerable<IDatabase> databases, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.Redis.RedisDistributedReaderWriterLock.RedisDistributedReaderWriterLock(string,System.Collections.Generic.IEnumerable_IDatabase_,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.Redis.RedisDistributedReaderWriterLock.RedisDistributedReaderWriterLock(string,System.Collections.Generic.IEnumerable_IDatabase_,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).databases'></a>

`databases` [System.Collections.Generic.IEnumerable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IEnumerable-1 'System.Collections.Generic.IEnumerable`1')[StackExchange.Redis.IDatabase](https://docs.microsoft.com/en-us/dotnet/api/StackExchange.Redis.IDatabase 'StackExchange.Redis.IDatabase')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IEnumerable-1 'System.Collections.Generic.IEnumerable`1')

<a name='Medallion.Threading.Redis.RedisDistributedReaderWriterLock.RedisDistributedReaderWriterLock(string,System.Collections.Generic.IEnumerable_IDatabase_,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[RedisDistributedSynchronizationOptionsBuilder](RedisDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')