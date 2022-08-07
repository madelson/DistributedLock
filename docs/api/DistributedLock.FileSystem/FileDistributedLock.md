#### [DistributedLock.FileSystem](README.md 'README')
### [Medallion.Threading.FileSystem](Medallion.Threading.FileSystem.md 'Medallion.Threading.FileSystem')

## FileDistributedLock Class

A distributed lock based on holding an exclusive handle to a lock file. The file will be deleted when the lock is released.

```csharp
public sealed class FileDistributedLock :
Medallion.Threading.IDistributedLock
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; FileDistributedLock

Implements [IDistributedLock](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLock.md 'Medallion.Threading.IDistributedLock')

| Constructors | |
| :--- | :--- |
| [FileDistributedLock(DirectoryInfo, string)](FileDistributedLock..ctor.vNzzxMsPVMiUjD+hEvX7EQ.md 'Medallion.Threading.FileSystem.FileDistributedLock.FileDistributedLock(System.IO.DirectoryInfo, string)') | Constructs a lock which will place a lock file in [lockFileDirectory](FileDistributedLock..ctor.vNzzxMsPVMiUjD+hEvX7EQ.md#Medallion.Threading.FileSystem.FileDistributedLock.FileDistributedLock(System.IO.DirectoryInfo,string).lockFileDirectory 'Medallion.Threading.FileSystem.FileDistributedLock.FileDistributedLock(System.IO.DirectoryInfo, string).lockFileDirectory'). The file's name<br/>will be based on [name](FileDistributedLock..ctor.vNzzxMsPVMiUjD+hEvX7EQ.md#Medallion.Threading.FileSystem.FileDistributedLock.FileDistributedLock(System.IO.DirectoryInfo,string).name 'Medallion.Threading.FileSystem.FileDistributedLock.FileDistributedLock(System.IO.DirectoryInfo, string).name'), but with proper escaping/hashing to ensure that a valid file name is produced.<br/><br/>Upon acquiring the lock, the file's directory will be created automatically if it does not already exist. The file <br/>will similarly be created if it does not already exist, and will be deleted when the lock is released. |
| [FileDistributedLock(FileInfo)](FileDistributedLock..ctor./N7mTMuqXSs9QBL49fhQcQ.md 'Medallion.Threading.FileSystem.FileDistributedLock.FileDistributedLock(System.IO.FileInfo)') | Constructs a lock which uses the provided [lockFile](FileDistributedLock..ctor./N7mTMuqXSs9QBL49fhQcQ.md#Medallion.Threading.FileSystem.FileDistributedLock.FileDistributedLock(System.IO.FileInfo).lockFile 'Medallion.Threading.FileSystem.FileDistributedLock.FileDistributedLock(System.IO.FileInfo).lockFile') as the exact file name.<br/><br/>Upon acquiring the lock, the file's directory will be created automatically if it does not already exist. The file <br/>will similarly be created if it does not already exist, and will be deleted when the lock is released. |

| Properties | |
| :--- | :--- |
| [Name](FileDistributedLock.Name.md 'Medallion.Threading.FileSystem.FileDistributedLock.Name') | Implements [Name](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLock.Name.md 'Medallion.Threading.IDistributedLock.Name') |

| Methods | |
| :--- | :--- |
| [Acquire(Nullable&lt;TimeSpan&gt;, CancellationToken)](FileDistributedLock.Acquire.Cxe4kn5Dotos0VNT8S7mFg.md 'Medallion.Threading.FileSystem.FileDistributedLock.Acquire(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires the lock synchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: |
| [AcquireAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](FileDistributedLock.AcquireAsync.S5lCsEuKHmzK+f7lUXq+aw.md 'Medallion.Threading.FileSystem.FileDistributedLock.AcquireAsync(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires the lock asynchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: |
| [TryAcquire(TimeSpan, CancellationToken)](FileDistributedLock.TryAcquire.zN8fZOiOkhzpcu5NxeJk8g.md 'Medallion.Threading.FileSystem.FileDistributedLock.TryAcquire(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire the lock synchronously. Usage: |
| [TryAcquireAsync(TimeSpan, CancellationToken)](FileDistributedLock.TryAcquireAsync.VuLjCVM0f9HO9kvR8wPWxQ.md 'Medallion.Threading.FileSystem.FileDistributedLock.TryAcquireAsync(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire the lock asynchronously. Usage: |
