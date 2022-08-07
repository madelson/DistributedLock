#### [DistributedLock.ZooKeeper](README.md 'README')
### [Medallion.Threading.ZooKeeper](Medallion.Threading.ZooKeeper.md 'Medallion.Threading.ZooKeeper').[ZooKeeperDistributedSemaphore](ZooKeeperDistributedSemaphore.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSemaphore')

## ZooKeeperDistributedSemaphore.Medallion.Threading.IDistributedSemaphore.Name Property

Implements [Name](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSemaphore.Name.md 'Medallion.Threading.IDistributedSemaphore.Name'). Implemented explicitly to avoid confusion with the fact  
that this will include the leading "/" and base directory alongside the passed-in name.

```csharp
string Medallion.Threading.IDistributedSemaphore.Name { get; }
```

Implements [Name](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSemaphore.Name.md 'Medallion.Threading.IDistributedSemaphore.Name')