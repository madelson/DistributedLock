#### [DistributedLock.Postgres](README.md 'README')
### [Medallion.Threading.Postgres](Medallion.Threading.Postgres.md 'Medallion.Threading.Postgres').[PostgresDistributedSynchronizationProvider](PostgresDistributedSynchronizationProvider.md 'Medallion.Threading.Postgres.PostgresDistributedSynchronizationProvider')

## PostgresDistributedSynchronizationProvider(string, Action<PostgresConnectionOptionsBuilder>) Constructor

Constructs a provider which connects to Postgres using the provided [connectionString](PostgresDistributedSynchronizationProvider..ctor.eWR3ijjn8fn/O9iDMaj84A.md#Medallion.Threading.Postgres.PostgresDistributedSynchronizationProvider.PostgresDistributedSynchronizationProvider(string,System.Action_Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder_).connectionString 'Medallion.Threading.Postgres.PostgresDistributedSynchronizationProvider.PostgresDistributedSynchronizationProvider(string, System.Action<Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder>).connectionString') and [options](PostgresDistributedSynchronizationProvider..ctor.eWR3ijjn8fn/O9iDMaj84A.md#Medallion.Threading.Postgres.PostgresDistributedSynchronizationProvider.PostgresDistributedSynchronizationProvider(string,System.Action_Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder_).options 'Medallion.Threading.Postgres.PostgresDistributedSynchronizationProvider.PostgresDistributedSynchronizationProvider(string, System.Action<Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder>).options').

```csharp
public PostgresDistributedSynchronizationProvider(string connectionString, System.Action<Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.Postgres.PostgresDistributedSynchronizationProvider.PostgresDistributedSynchronizationProvider(string,System.Action_Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder_).connectionString'></a>

`connectionString` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.Postgres.PostgresDistributedSynchronizationProvider.PostgresDistributedSynchronizationProvider(string,System.Action_Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[PostgresConnectionOptionsBuilder](PostgresConnectionOptionsBuilder.md 'Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')