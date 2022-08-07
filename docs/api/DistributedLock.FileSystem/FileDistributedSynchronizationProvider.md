#### [DistributedLock.FileSystem](README.md 'README')
### [Medallion.Threading.FileSystem](Medallion.Threading.FileSystem.md 'Medallion.Threading.FileSystem')

## FileDistributedSynchronizationProvider Class

Implements [IDistributedLockProvider](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockProvider.md 'Medallion.Threading.IDistributedLockProvider') for [FileDistributedLock](FileDistributedLock.md 'Medallion.Threading.FileSystem.FileDistributedLock')

```csharp
public sealed class FileDistributedSynchronizationProvider :
Medallion.Threading.IDistributedLockProvider
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; FileDistributedSynchronizationProvider

Implements [IDistributedLockProvider](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockProvider.md 'Medallion.Threading.IDistributedLockProvider')

| Constructors | |
| :--- | :--- |
| [FileDistributedSynchronizationProvider(DirectoryInfo)](FileDistributedSynchronizationProvider..ctor.TETT6F6M/CskpBpX2hNqtA.md 'Medallion.Threading.FileSystem.FileDistributedSynchronizationProvider.FileDistributedSynchronizationProvider(System.IO.DirectoryInfo)') | Constructs a provider that scopes lock files within the provided [lockFileDirectory](FileDistributedSynchronizationProvider..ctor.TETT6F6M/CskpBpX2hNqtA.md#Medallion.Threading.FileSystem.FileDistributedSynchronizationProvider.FileDistributedSynchronizationProvider(System.IO.DirectoryInfo).lockFileDirectory 'Medallion.Threading.FileSystem.FileDistributedSynchronizationProvider.FileDistributedSynchronizationProvider(System.IO.DirectoryInfo).lockFileDirectory'). |

| Methods | |
| :--- | :--- |
| [CreateLock(string)](FileDistributedSynchronizationProvider.CreateLock.bu/BmarBn5NPtnEydKr3pQ.md 'Medallion.Threading.FileSystem.FileDistributedSynchronizationProvider.CreateLock(string)') | Constructs a [FileDistributedLock](FileDistributedLock.md 'Medallion.Threading.FileSystem.FileDistributedLock') with the given [name](FileDistributedSynchronizationProvider.CreateLock.bu/BmarBn5NPtnEydKr3pQ.md#Medallion.Threading.FileSystem.FileDistributedSynchronizationProvider.CreateLock(string).name 'Medallion.Threading.FileSystem.FileDistributedSynchronizationProvider.CreateLock(string).name'). |
