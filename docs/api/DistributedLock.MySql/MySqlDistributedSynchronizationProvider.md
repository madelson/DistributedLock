#### [DistributedLock.MySql](README.md 'README')
### [Medallion.Threading.MySql](Medallion.Threading.MySql.md 'Medallion.Threading.MySql')

## MySqlDistributedSynchronizationProvider Class

Implements [IDistributedLockProvider](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockProvider.md 'Medallion.Threading.IDistributedLockProvider') for [MySqlDistributedLock](MySqlDistributedLock.md 'Medallion.Threading.MySql.MySqlDistributedLock')

```csharp
public sealed class MySqlDistributedSynchronizationProvider :
Medallion.Threading.IDistributedLockProvider
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; MySqlDistributedSynchronizationProvider

Implements [IDistributedLockProvider](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockProvider.md 'Medallion.Threading.IDistributedLockProvider')

| Constructors | |
| :--- | :--- |
| [MySqlDistributedSynchronizationProvider(string, Action&lt;MySqlConnectionOptionsBuilder&gt;)](MySqlDistributedSynchronizationProvider..ctor.//nOe7Is6T7o1EV/QK7Yew.md 'Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.MySqlDistributedSynchronizationProvider(string, System.Action<Medallion.Threading.MySql.MySqlConnectionOptionsBuilder>)') | Constructs a provider that connects with [connectionString](MySqlDistributedSynchronizationProvider..ctor.//nOe7Is6T7o1EV/QK7Yew.md#Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.MySqlDistributedSynchronizationProvider(string,System.Action_Medallion.Threading.MySql.MySqlConnectionOptionsBuilder_).connectionString 'Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.MySqlDistributedSynchronizationProvider(string, System.Action<Medallion.Threading.MySql.MySqlConnectionOptionsBuilder>).connectionString') and [options](MySqlDistributedSynchronizationProvider..ctor.//nOe7Is6T7o1EV/QK7Yew.md#Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.MySqlDistributedSynchronizationProvider(string,System.Action_Medallion.Threading.MySql.MySqlConnectionOptionsBuilder_).options 'Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.MySqlDistributedSynchronizationProvider(string, System.Action<Medallion.Threading.MySql.MySqlConnectionOptionsBuilder>).options'). |
| [MySqlDistributedSynchronizationProvider(IDbConnection)](MySqlDistributedSynchronizationProvider..ctor.pke77kyAr68Nsic0RCNm1g.md 'Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.MySqlDistributedSynchronizationProvider(System.Data.IDbConnection)') | Constructs a provider that connects with [connection](MySqlDistributedSynchronizationProvider..ctor.pke77kyAr68Nsic0RCNm1g.md#Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.MySqlDistributedSynchronizationProvider(System.Data.IDbConnection).connection 'Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.MySqlDistributedSynchronizationProvider(System.Data.IDbConnection).connection'). |
| [MySqlDistributedSynchronizationProvider(IDbTransaction)](MySqlDistributedSynchronizationProvider..ctor.p7z4Ra8yKm8ddj1YbCV32w.md 'Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.MySqlDistributedSynchronizationProvider(System.Data.IDbTransaction)') | Constructs a provider that connects with [transaction](MySqlDistributedSynchronizationProvider..ctor.p7z4Ra8yKm8ddj1YbCV32w.md#Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.MySqlDistributedSynchronizationProvider(System.Data.IDbTransaction).transaction 'Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.MySqlDistributedSynchronizationProvider(System.Data.IDbTransaction).transaction'). |

| Methods | |
| :--- | :--- |
| [CreateLock(string, bool)](MySqlDistributedSynchronizationProvider.CreateLock.BfFkX376FOT5FK96U7Fr1g.md 'Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.CreateLock(string, bool)') | Creates a [MySqlDistributedLock](MySqlDistributedLock.md 'Medallion.Threading.MySql.MySqlDistributedLock') with the provided [name](MySqlDistributedSynchronizationProvider.CreateLock.BfFkX376FOT5FK96U7Fr1g.md#Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.CreateLock(string,bool).name 'Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.CreateLock(string, bool).name'). Unless [exactName](MySqlDistributedSynchronizationProvider.CreateLock.BfFkX376FOT5FK96U7Fr1g.md#Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.CreateLock(string,bool).exactName 'Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.CreateLock(string, bool).exactName')  is specified, invalid names will be escaped/hashed. |
