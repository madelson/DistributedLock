#### [DistributedLock.ZooKeeper](README.md 'README')
### [Medallion.Threading.ZooKeeper](Medallion.Threading.ZooKeeper.md 'Medallion.Threading.ZooKeeper').[ZooKeeperDistributedLock](ZooKeeperDistributedLock.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedLock')

## ZooKeeperDistributedLock.Medallion.Threading.IDistributedLock.Name Property

Implements [Name](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLock.Name.md 'Medallion.Threading.IDistributedLock.Name'). Implemented explicitly to avoid confusion with the fact  
that this will include the leading "/" and base directory alongside the passed-in name.

```csharp
string Medallion.Threading.IDistributedLock.Name { get; }
```

Implements [Name](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLock.Name.md 'Medallion.Threading.IDistributedLock.Name')