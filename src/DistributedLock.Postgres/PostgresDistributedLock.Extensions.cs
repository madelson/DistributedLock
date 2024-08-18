using Medallion.Threading.Internal;
using System.Data;

namespace Medallion.Threading.Postgres;

public partial class PostgresDistributedLock
{
    public static bool TryAcquireWithTransaction(PostgresAdvisoryLockKey key, IDbTransaction transaction, TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        if (key == null) { throw new ArgumentNullException(nameof(key)); }
        if (transaction == null) { throw new ArgumentNullException(nameof(transaction)); }

        var connection = new PostgresDatabaseConnection(transaction);

        var handle = DistributedLockHelpers.TryAcquire(PostgresAdvisoryLock.ExclusiveLock, connection, key.ToString(), timeout, cancellationToken);

        return handle != null;
    }

    public static void AcquireWithTransaction(PostgresAdvisoryLockKey key, IDbTransaction transaction, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        if (key == null) { throw new ArgumentNullException(nameof(key)); }
        if (transaction == null) { throw new ArgumentNullException(nameof(transaction)); }

        var connection = new PostgresDatabaseConnection(transaction);

        DistributedLockHelpers.Acquire(PostgresAdvisoryLock.ExclusiveLock, connection, key.ToString(), timeout, cancellationToken);
    }

    public static ValueTask<bool> TryAcquireWithTransactionAsync(PostgresAdvisoryLockKey key, IDbTransaction transaction, TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        if (key == null) { throw new ArgumentNullException(nameof(key)); }
        if (transaction == null) { throw new ArgumentNullException(nameof(transaction)); }

        var connection = new PostgresDatabaseConnection(transaction);

        return TryAcquireAsync();

        async ValueTask<bool> TryAcquireAsync()
        {
            var handle = await DistributedLockHelpers.TryAcquireAsync(PostgresAdvisoryLock.ExclusiveLock, connection, key.ToString(), timeout, cancellationToken);

            return handle != null;
        }
    }

    public static ValueTask AcquireWithTransactionAsync(PostgresAdvisoryLockKey key, IDbTransaction transaction, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        if (key == null) { throw new ArgumentNullException(nameof(key)); }
        if (transaction == null) { throw new ArgumentNullException(nameof(transaction)); }

        var connection = new PostgresDatabaseConnection(transaction);

        return DistributedLockHelpers.AcquireAsync(PostgresAdvisoryLock.ExclusiveLock, connection, key.ToString(), timeout, cancellationToken).ConvertToVoid();
    }
}
