### [Medallion.Threading.WaitHandles](0cv6wmZCIva5FK3cOR8t5g.md 'Medallion.Threading.WaitHandles').[WaitHandleDistributedSemaphore](u++QKv1+Gje9fkJlbjceow.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore')

## WaitHandleDistributedSemaphore(string, int, Nullable<TimeSpan>, bool) Constructor

Constructs a lock with the given [name](4ysEtsHFYQ8UNvnVKspCjg.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string,int,System.Nullable_System.TimeSpan_,bool).name 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string, int, System.Nullable<System.TimeSpan>, bool).name').  
  
[abandonmentCheckCadence](4ysEtsHFYQ8UNvnVKspCjg.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string,int,System.Nullable_System.TimeSpan_,bool).abandonmentCheckCadence 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string, int, System.Nullable<System.TimeSpan>, bool).abandonmentCheckCadence') specifies how frequently we refresh our [System.Threading.Semaphore](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Semaphore 'System.Threading.Semaphore') object in case it is abandoned by  
its original owner. The default is 2s.  
  
Unless [exactName](4ysEtsHFYQ8UNvnVKspCjg.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string,int,System.Nullable_System.TimeSpan_,bool).exactName 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string, int, System.Nullable<System.TimeSpan>, bool).exactName') is specified, [name](4ysEtsHFYQ8UNvnVKspCjg.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string,int,System.Nullable_System.TimeSpan_,bool).name 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string, int, System.Nullable<System.TimeSpan>, bool).name') will be escaped/hashed to ensure name validity.

```csharp
public WaitHandleDistributedSemaphore(string name, int maxCount, System.Nullable<System.TimeSpan> abandonmentCheckCadence=null, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string,int,System.Nullable_System.TimeSpan_,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string,int,System.Nullable_System.TimeSpan_,bool).maxCount'></a>

`maxCount` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string,int,System.Nullable_System.TimeSpan_,bool).abandonmentCheckCadence'></a>

`abandonmentCheckCadence` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')[System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.WaitHandleDistributedSemaphore(string,int,System.Nullable_System.TimeSpan_,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')