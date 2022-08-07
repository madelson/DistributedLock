#### [DistributedLock.FileSystem](README.md 'README')

## Medallion.Threading.FileSystem Namespace

| Classes | |
| :--- | :--- |
| [FileDistributedLock](FileDistributedLock.md 'Medallion.Threading.FileSystem.FileDistributedLock') | A distributed lock based on holding an exclusive handle to a lock file. The file will be deleted when the lock is released. |
| [FileDistributedLockHandle](FileDistributedLockHandle.md 'Medallion.Threading.FileSystem.FileDistributedLockHandle') | Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle') |
| [FileDistributedSynchronizationProvider](FileDistributedSynchronizationProvider.md 'Medallion.Threading.FileSystem.FileDistributedSynchronizationProvider') | Implements [IDistributedLockProvider](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockProvider.md 'Medallion.Threading.IDistributedLockProvider') for [FileDistributedLock](FileDistributedLock.md 'Medallion.Threading.FileSystem.FileDistributedLock') |
