#### [DistributedLock.ZooKeeper](README.md 'README')
### [Medallion.Threading.ZooKeeper](Medallion.Threading.ZooKeeper.md 'Medallion.Threading.ZooKeeper').[ZooKeeperDistributedReaderWriterLock](ZooKeeperDistributedReaderWriterLock.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock')

## ZooKeeperDistributedReaderWriterLock.Medallion.Threading.IDistributedReaderWriterLock.Name Property

Implements [Name](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedReaderWriterLock.Name.md 'Medallion.Threading.IDistributedReaderWriterLock.Name'). Implemented explicitly to avoid confusion with the fact
that this will include the leading "/" and base directory alongside the passed-in name.

```csharp
string Medallion.Threading.IDistributedReaderWriterLock.Name { get; }
```

Implements [Name](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedReaderWriterLock.Name.md 'Medallion.Threading.IDistributedReaderWriterLock.Name')