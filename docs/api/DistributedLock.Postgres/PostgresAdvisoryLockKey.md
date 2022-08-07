#### [DistributedLock.Postgres](README.md 'README')
### [Medallion.Threading.Postgres](Medallion.Threading.Postgres.md 'Medallion.Threading.Postgres')

## PostgresAdvisoryLockKey Struct

Acts as the "name" of a distributed lock in Postgres. Consists of one 64-bit value or two 32-bit values (the spaces do not overlap).  
See https://www.postgresql.org/docs/12/functions-admin.html#FUNCTIONS-ADVISORY-LOCKS

```csharp
public readonly struct PostgresAdvisoryLockKey :
System.IEquatable<Medallion.Threading.Postgres.PostgresAdvisoryLockKey>
```

Implements [System.IEquatable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.IEquatable-1 'System.IEquatable`1')[PostgresAdvisoryLockKey](PostgresAdvisoryLockKey.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.IEquatable-1 'System.IEquatable`1')

| Constructors | |
| :--- | :--- |
| [PostgresAdvisoryLockKey(int, int)](PostgresAdvisoryLockKey..ctor.LedpDNSMoNGTkn636aYbow.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(int, int)') | Constructs a key from a pair of 32-bit values. This is a separate key space <br/>than [PostgresAdvisoryLockKey(long)](PostgresAdvisoryLockKey..ctor.zWR+TwIbSGzvQzL9/uWeBQ.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(long)'). |
| [PostgresAdvisoryLockKey(long)](PostgresAdvisoryLockKey..ctor.zWR+TwIbSGzvQzL9/uWeBQ.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(long)') | Constructs a key from a single 64-bit value. This is a separate key space <br/>than [PostgresAdvisoryLockKey(int, int)](PostgresAdvisoryLockKey..ctor.LedpDNSMoNGTkn636aYbow.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(int, int)'). |
| [PostgresAdvisoryLockKey(string, bool)](PostgresAdvisoryLockKey..ctor.nrSDuGGKUsKtcB73EU1nXg.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(string, bool)') | Constructs a key based on a string [name](PostgresAdvisoryLockKey..ctor.nrSDuGGKUsKtcB73EU1nXg.md#Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(string,bool).name 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(string, bool).name').<br/><br/>If the string is of the form "{16-digit hex}" or "{8-digit hex},{8-digit hex}", this will be parsed into numeric keys.<br/><br/>If the string is an ascii string with 9 or fewer characters, it will be mapped to a key that does not collide with<br/>any other key based on such a string or based on a 32-bit value.<br/><br/>Other string names will be rejected unless [allowHashing](PostgresAdvisoryLockKey..ctor.nrSDuGGKUsKtcB73EU1nXg.md#Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(string,bool).allowHashing 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(string, bool).allowHashing') is specified, in which case it will be hashed to<br/>a 64-bit key value. |

| Methods | |
| :--- | :--- |
| [Equals(PostgresAdvisoryLockKey)](PostgresAdvisoryLockKey.Equals.+QU35/RcjHTGcjW0iCLCkA.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.Equals(Medallion.Threading.Postgres.PostgresAdvisoryLockKey)') | Implements [System.IEquatable&lt;&gt;.Equals(@0)](https://docs.microsoft.com/en-us/dotnet/api/System.IEquatable-1.Equals#System_IEquatable_1_Equals__0_ 'System.IEquatable`1.Equals(`0)') |
| [Equals(object)](PostgresAdvisoryLockKey.Equals.hCSLKXsGZ9vchAeOS3hn/Q.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.Equals(object)') | Implements [System.Object.Equals(System.Object)](https://docs.microsoft.com/en-us/dotnet/api/System.Object.Equals#System_Object_Equals_System_Object_ 'System.Object.Equals(System.Object)') |
| [GetHashCode()](PostgresAdvisoryLockKey.GetHashCode().md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.GetHashCode()') | Implements [System.Object.GetHashCode](https://docs.microsoft.com/en-us/dotnet/api/System.Object.GetHashCode 'System.Object.GetHashCode') |
| [ToString()](PostgresAdvisoryLockKey.ToString().md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.ToString()') | Returns a string representation of the key that can be round-tripped through<br/>[PostgresAdvisoryLockKey(string, bool)](PostgresAdvisoryLockKey..ctor.nrSDuGGKUsKtcB73EU1nXg.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(string, bool)') |

| Operators | |
| :--- | :--- |
| [operator ==(PostgresAdvisoryLockKey, PostgresAdvisoryLockKey)](PostgresAdvisoryLockKey.op_Equality.WzLfN5crVgBoMiN0RMIAhA.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.op_Equality(Medallion.Threading.Postgres.PostgresAdvisoryLockKey, Medallion.Threading.Postgres.PostgresAdvisoryLockKey)') | Provides equality based on [Equals(PostgresAdvisoryLockKey)](PostgresAdvisoryLockKey.Equals.+QU35/RcjHTGcjW0iCLCkA.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.Equals(Medallion.Threading.Postgres.PostgresAdvisoryLockKey)') |
| [operator !=(PostgresAdvisoryLockKey, PostgresAdvisoryLockKey)](PostgresAdvisoryLockKey.op_Inequality.QLx2INIQzAIzn1yA2+FG0Q.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.op_Inequality(Medallion.Threading.Postgres.PostgresAdvisoryLockKey, Medallion.Threading.Postgres.PostgresAdvisoryLockKey)') | Provides inequality based on [Equals(PostgresAdvisoryLockKey)](PostgresAdvisoryLockKey.Equals.+QU35/RcjHTGcjW0iCLCkA.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.Equals(Medallion.Threading.Postgres.PostgresAdvisoryLockKey)') |
