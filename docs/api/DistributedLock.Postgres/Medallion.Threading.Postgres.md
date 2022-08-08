#### [DistributedLock.Postgres](README.md 'README')

## Medallion.Threading.Postgres Namespace

| Classes | |
| :--- | :--- |
| [PostgresConnectionOptionsBuilder](PostgresConnectionOptionsBuilder.md 'Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder') | Specifies options for connecting to and locking against a Postgres database |
| [PostgresDistributedLock](PostgresDistributedLock.md 'Medallion.Threading.Postgres.PostgresDistributedLock') | Implements a distributed lock using Postgres advisory locks (see https://www.postgresql.org/docs/12/functions-admin.html#FUNCTIONS-ADVISORY-LOCKS) |
| [PostgresDistributedLockHandle](PostgresDistributedLockHandle.md 'Medallion.Threading.Postgres.PostgresDistributedLockHandle') | Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle') |
| [PostgresDistributedReaderWriterLock](PostgresDistributedReaderWriterLock.md 'Medallion.Threading.Postgres.PostgresDistributedReaderWriterLock') | Implements a distributed lock using Postgres advisory locks (see https://www.postgresql.org/docs/12/functions-admin.html#FUNCTIONS-ADVISORY-LOCKS) |
| [PostgresDistributedReaderWriterLockHandle](PostgresDistributedReaderWriterLockHandle.md 'Medallion.Threading.Postgres.PostgresDistributedReaderWriterLockHandle') | Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle') |
| [PostgresDistributedSynchronizationProvider](PostgresDistributedSynchronizationProvider.md 'Medallion.Threading.Postgres.PostgresDistributedSynchronizationProvider') | Implements [IDistributedLockProvider](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockProvider.md 'Medallion.Threading.IDistributedLockProvider') for [PostgresDistributedLock](PostgresDistributedLock.md 'Medallion.Threading.Postgres.PostgresDistributedLock') and [IDistributedReaderWriterLockProvider](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedReaderWriterLockProvider.md 'Medallion.Threading.IDistributedReaderWriterLockProvider') for [PostgresDistributedReaderWriterLock](PostgresDistributedReaderWriterLock.md 'Medallion.Threading.Postgres.PostgresDistributedReaderWriterLock'). |

| Structs | |
| :--- | :--- |
| [PostgresAdvisoryLockKey](PostgresAdvisoryLockKey.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey') | Acts as the "name" of a distributed lock in Postgres. Consists of one 64-bit value or two 32-bit values (the spaces do not overlap). See https://www.postgresql.org/docs/12/functions-admin.html#FUNCTIONS-ADVISORY-LOCKS |
