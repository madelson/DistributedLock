#### [DistributedLock.FileSystem](README.md 'README')
### [Medallion.Threading.FileSystem](Medallion.Threading.FileSystem.md 'Medallion.Threading.FileSystem').[FileDistributedLock](FileDistributedLock.md 'Medallion.Threading.FileSystem.FileDistributedLock')

## FileDistributedLock(FileInfo) Constructor

Constructs a lock which uses the provided [lockFile](FileDistributedLock..ctor./N7mTMuqXSs9QBL49fhQcQ.md#Medallion.Threading.FileSystem.FileDistributedLock.FileDistributedLock(System.IO.FileInfo).lockFile 'Medallion.Threading.FileSystem.FileDistributedLock.FileDistributedLock(System.IO.FileInfo).lockFile') as the exact file name.  
  
Upon acquiring the lock, the file's directory will be created automatically if it does not already exist. The file   
will similarly be created if it does not already exist, and will be deleted when the lock is released.

```csharp
public FileDistributedLock(System.IO.FileInfo lockFile);
```
#### Parameters

<a name='Medallion.Threading.FileSystem.FileDistributedLock.FileDistributedLock(System.IO.FileInfo).lockFile'></a>

`lockFile` [System.IO.FileInfo](https://docs.microsoft.com/en-us/dotnet/api/System.IO.FileInfo 'System.IO.FileInfo')