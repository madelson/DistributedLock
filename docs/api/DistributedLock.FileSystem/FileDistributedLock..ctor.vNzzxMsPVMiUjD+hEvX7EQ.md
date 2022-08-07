#### [DistributedLock.FileSystem](README.md 'README')
### [Medallion.Threading.FileSystem](Medallion.Threading.FileSystem.md 'Medallion.Threading.FileSystem').[FileDistributedLock](FileDistributedLock.md 'Medallion.Threading.FileSystem.FileDistributedLock')

## FileDistributedLock(DirectoryInfo, string) Constructor

Constructs a lock which will place a lock file in [lockFileDirectory](FileDistributedLock..ctor.vNzzxMsPVMiUjD+hEvX7EQ.md#Medallion.Threading.FileSystem.FileDistributedLock.FileDistributedLock(System.IO.DirectoryInfo,string).lockFileDirectory 'Medallion.Threading.FileSystem.FileDistributedLock.FileDistributedLock(System.IO.DirectoryInfo, string).lockFileDirectory'). The file's name  
will be based on [name](FileDistributedLock..ctor.vNzzxMsPVMiUjD+hEvX7EQ.md#Medallion.Threading.FileSystem.FileDistributedLock.FileDistributedLock(System.IO.DirectoryInfo,string).name 'Medallion.Threading.FileSystem.FileDistributedLock.FileDistributedLock(System.IO.DirectoryInfo, string).name'), but with proper escaping/hashing to ensure that a valid file name is produced.  
  
Upon acquiring the lock, the file's directory will be created automatically if it does not already exist. The file   
will similarly be created if it does not already exist, and will be deleted when the lock is released.

```csharp
public FileDistributedLock(System.IO.DirectoryInfo lockFileDirectory, string name);
```
#### Parameters

<a name='Medallion.Threading.FileSystem.FileDistributedLock.FileDistributedLock(System.IO.DirectoryInfo,string).lockFileDirectory'></a>

`lockFileDirectory` [System.IO.DirectoryInfo](https://docs.microsoft.com/en-us/dotnet/api/System.IO.DirectoryInfo 'System.IO.DirectoryInfo')

<a name='Medallion.Threading.FileSystem.FileDistributedLock.FileDistributedLock(System.IO.DirectoryInfo,string).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')