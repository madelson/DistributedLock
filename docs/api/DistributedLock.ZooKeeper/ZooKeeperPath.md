#### [DistributedLock.ZooKeeper](README.md 'README')
### [Medallion.Threading.ZooKeeper](Medallion.Threading.ZooKeeper.md 'Medallion.Threading.ZooKeeper')

## ZooKeeperPath Struct

Represents a path to a ZooKeeper node. The constructor validates that the input is a valid path.  
Call [ToString()](ZooKeeperPath.ToString().md 'Medallion.Threading.ZooKeeper.ZooKeeperPath.ToString()') to get the path value.

```csharp
public readonly struct ZooKeeperPath :
System.IEquatable<Medallion.Threading.ZooKeeper.ZooKeeperPath>
```

Implements [System.IEquatable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.IEquatable-1 'System.IEquatable`1')[ZooKeeperPath](ZooKeeperPath.md 'Medallion.Threading.ZooKeeper.ZooKeeperPath')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.IEquatable-1 'System.IEquatable`1')

| Constructors | |
| :--- | :--- |
| [ZooKeeperPath(string)](ZooKeeperPath..ctor.ZsFWJSkmkGB08MbVdjFPyw.md 'Medallion.Threading.ZooKeeper.ZooKeeperPath.ZooKeeperPath(string)') | Constructs a new [ZooKeeperPath](ZooKeeperPath.md 'Medallion.Threading.ZooKeeper.ZooKeeperPath') based on the given [path](ZooKeeperPath..ctor.ZsFWJSkmkGB08MbVdjFPyw.md#Medallion.Threading.ZooKeeper.ZooKeeperPath.ZooKeeperPath(string).path 'Medallion.Threading.ZooKeeper.ZooKeeperPath.ZooKeeperPath(string).path') string. |

| Methods | |
| :--- | :--- |
| [Equals(ZooKeeperPath)](ZooKeeperPath.Equals.VVXwkPSRrVJ19AkTFg5+xw.md 'Medallion.Threading.ZooKeeper.ZooKeeperPath.Equals(Medallion.Threading.ZooKeeper.ZooKeeperPath)') | Implements equality based on the path string |
| [Equals(object)](ZooKeeperPath.Equals.J5/wqdQppznvVwiU3kq31Q.md 'Medallion.Threading.ZooKeeper.ZooKeeperPath.Equals(object)') | Implements equality based on the path string |
| [GetHashCode()](ZooKeeperPath.GetHashCode().md 'Medallion.Threading.ZooKeeper.ZooKeeperPath.GetHashCode()') | Implements hashing based on the path string |
| [ToString()](ZooKeeperPath.ToString().md 'Medallion.Threading.ZooKeeper.ZooKeeperPath.ToString()') | Returns the path value as a string |

| Operators | |
| :--- | :--- |
| [operator ==(ZooKeeperPath, ZooKeeperPath)](ZooKeeperPath.op_Equality.0Gh9Mk6h/aa6pYstqDJebg.md 'Medallion.Threading.ZooKeeper.ZooKeeperPath.op_Equality(Medallion.Threading.ZooKeeper.ZooKeeperPath, Medallion.Threading.ZooKeeper.ZooKeeperPath)') | Implements equality based on the path string |
| [operator !=(ZooKeeperPath, ZooKeeperPath)](ZooKeeperPath.op_Inequality.hyYJhclLXAbXbwzJmpT2JA.md 'Medallion.Threading.ZooKeeper.ZooKeeperPath.op_Inequality(Medallion.Threading.ZooKeeper.ZooKeeperPath, Medallion.Threading.ZooKeeper.ZooKeeperPath)') | Implements inequality based on the path string |
