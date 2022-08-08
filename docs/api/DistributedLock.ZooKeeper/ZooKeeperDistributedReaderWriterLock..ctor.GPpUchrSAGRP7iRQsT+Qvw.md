#### [DistributedLock.ZooKeeper](README.md 'README')
### [Medallion.Threading.ZooKeeper](Medallion.Threading.ZooKeeper.md 'Medallion.Threading.ZooKeeper').[ZooKeeperDistributedReaderWriterLock](ZooKeeperDistributedReaderWriterLock.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock')

## ZooKeeperDistributedReaderWriterLock(ZooKeeperPath, string, bool, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>) Constructor

Constructs a new lock based on the provided [path](ZooKeeperDistributedReaderWriterLock..ctor.GPpUchrSAGRP7iRQsT+Qvw.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).path 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(Medallion.Threading.ZooKeeper.ZooKeeperPath, string, bool, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).path'), [connectionString](ZooKeeperDistributedReaderWriterLock..ctor.GPpUchrSAGRP7iRQsT+Qvw.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).connectionString 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(Medallion.Threading.ZooKeeper.ZooKeeperPath, string, bool, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).connectionString'), and [options](ZooKeeperDistributedReaderWriterLock..ctor.GPpUchrSAGRP7iRQsT+Qvw.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).options 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(Medallion.Threading.ZooKeeper.ZooKeeperPath, string, bool, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).options').

If [assumePathExists](ZooKeeperDistributedReaderWriterLock..ctor.GPpUchrSAGRP7iRQsT+Qvw.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).assumePathExists 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(Medallion.Threading.ZooKeeper.ZooKeeperPath, string, bool, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).assumePathExists') is specified, then the node will not be created as part of acquiring nor will it be 
deleted after releasing (defaults to false).

```csharp
public ZooKeeperDistributedReaderWriterLock(Medallion.Threading.ZooKeeper.ZooKeeperPath path, string connectionString, bool assumePathExists=false, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).path'></a>

`path` [ZooKeeperPath](ZooKeeperPath.md 'Medallion.Threading.ZooKeeper.ZooKeeperPath')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).connectionString'></a>

`connectionString` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).assumePathExists'></a>

`assumePathExists` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(Medallion.Threading.ZooKeeper.ZooKeeperPath,string,bool,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[ZooKeeperDistributedSynchronizationOptionsBuilder](ZooKeeperDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')