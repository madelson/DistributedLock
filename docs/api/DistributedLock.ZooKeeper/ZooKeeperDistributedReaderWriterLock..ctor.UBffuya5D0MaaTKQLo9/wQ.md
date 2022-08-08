#### [DistributedLock.ZooKeeper](README.md 'README')
### [Medallion.Threading.ZooKeeper](Medallion.Threading.ZooKeeper.md 'Medallion.Threading.ZooKeeper').[ZooKeeperDistributedReaderWriterLock](ZooKeeperDistributedReaderWriterLock.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock')

## ZooKeeperDistributedReaderWriterLock(string, string, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>) Constructor

Constructs a new lock based on the provided [name](ZooKeeperDistributedReaderWriterLock..ctor.UBffuya5D0MaaTKQLo9/wQ.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).name 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(string, string, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).name'), [connectionString](ZooKeeperDistributedReaderWriterLock..ctor.UBffuya5D0MaaTKQLo9/wQ.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).connectionString 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(string, string, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).connectionString'), and [options](ZooKeeperDistributedReaderWriterLock..ctor.UBffuya5D0MaaTKQLo9/wQ.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).options 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(string, string, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).options').

The lock's path will be a parent node of the root directory '/'. If [name](ZooKeeperDistributedReaderWriterLock..ctor.UBffuya5D0MaaTKQLo9/wQ.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).name 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(string, string, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>).name') is not a valid node name, it will be transformed to ensure
validity.

```csharp
public ZooKeeperDistributedReaderWriterLock(string name, string connectionString, System.Action<Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).connectionString'></a>

`connectionString` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.ZooKeeperDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[ZooKeeperDistributedSynchronizationOptionsBuilder](ZooKeeperDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')