#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading')

## DeadlockException Class

An exception that SOME distributed locks will throw under SOME deadlock conditions. Note that even locks
that throw this exception under some circumstances cannot detect ALL deadlock conditions

```csharp
public sealed class DeadlockException : System.InvalidOperationException
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [System.Exception](https://docs.microsoft.com/en-us/dotnet/api/System.Exception 'System.Exception') &#129106; [System.SystemException](https://docs.microsoft.com/en-us/dotnet/api/System.SystemException 'System.SystemException') &#129106; [System.InvalidOperationException](https://docs.microsoft.com/en-us/dotnet/api/System.InvalidOperationException 'System.InvalidOperationException') &#129106; DeadlockException

| Constructors | |
| :--- | :--- |
| [DeadlockException()](DeadlockException.DeadlockException().md 'Medallion.Threading.DeadlockException.DeadlockException()') | Constructs a new instance of [DeadlockException](DeadlockException.md 'Medallion.Threading.DeadlockException') with a default message |
| [DeadlockException(string, Exception)](DeadlockException..ctor.7kv3RcT81RlRFP35V3Vh0w.md 'Medallion.Threading.DeadlockException.DeadlockException(string, System.Exception)') | Constructs an instance of [DeadlockException](DeadlockException.md 'Medallion.Threading.DeadlockException') with the given [message](DeadlockException..ctor.7kv3RcT81RlRFP35V3Vh0w.md#Medallion.Threading.DeadlockException.DeadlockException(string,System.Exception).message 'Medallion.Threading.DeadlockException.DeadlockException(string, System.Exception).message') and [innerException](DeadlockException..ctor.7kv3RcT81RlRFP35V3Vh0w.md#Medallion.Threading.DeadlockException.DeadlockException(string,System.Exception).innerException 'Medallion.Threading.DeadlockException.DeadlockException(string, System.Exception).innerException') |
| [DeadlockException(string)](DeadlockException..ctor.rZAayxV0jwI4ZmlNNiyAyQ.md 'Medallion.Threading.DeadlockException.DeadlockException(string)') | Constructs an instance of [DeadlockException](DeadlockException.md 'Medallion.Threading.DeadlockException') with the given [message](DeadlockException..ctor.rZAayxV0jwI4ZmlNNiyAyQ.md#Medallion.Threading.DeadlockException.DeadlockException(string).message 'Medallion.Threading.DeadlockException.DeadlockException(string).message') |
