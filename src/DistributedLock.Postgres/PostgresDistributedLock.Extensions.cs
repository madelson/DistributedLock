using Medallion.Threading.Internal;
using System.Data;

namespace Medallion.Threading.Postgres;

public partial class PostgresDistributedLock
{
    /// <summary>
    /// Attempts to acquire a transaction-scoped advisory lock synchronously with an externally owned transaction. Usage: 
    /// <code>
    ///     var transaction = /* create a DB transaction */ 
    /// 
    ///     var isLockAcquired = myLock.TryAcquireWithTransaction(..., transaction, ...)
    ///
    ///     if (isLockAcquired != null) 
    ///     { 
    ///         /* we have the lock! */ 
    ///         
    ///         // Commit or Rollback the transaction, which in turn will release the lock
    ///     }
    /// </code>
    /// 
    /// NOTE: The owner of the transaction is the responsible party for it - the owner must commit or rollback the transaction in order to release the acquired lock.
    /// </summary>
    /// <param name="key">The postgres advisory lock key which will be used to acquire the lock.</param>
    /// <param name="transaction">The externally owned transaction which will be used to acquire the lock. The owner of the transaction must commit or rollback it for the lock to be released.</param>
    /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to 0.</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
    /// <returns>Whether the lock has been acquired</returns>
    public static bool TryAcquireWithTransaction(PostgresAdvisoryLockKey key, IDbTransaction transaction, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(state => TryAcquireWithTransactionAsyncInternal(state.key, state.transaction, state.timeout, state.cancellationToken), (key, transaction, timeout, cancellationToken));

    /// <summary>
    /// Acquires a transaction-scoped advisory lock synchronously, failing with <see cref="TimeoutException"/> if the attempt times out. Usage: 
    /// <code>
    ///     var transaction = /* create a DB transaction */ 
    /// 
    ///     myLock.AcquireWithTransaction(..., transaction, ...)
    ///
    ///     /* we have the lock! */ 
    ///         
    ///     // Commit or Rollback the transaction, which in turn will release the lock
    /// </code>
    /// 
    /// NOTE: The owner of the transaction is the responsible party for it - the owner must commit or rollback the transaction in order to release the acquired lock.
    /// </summary>
    /// <param name="key">The postgres advisory lock key which will be used to acquire the lock.</param>
    /// <param name="transaction">The externally owned transaction which will be used to acquire the lock. The owner of the transaction must commit or rollback it for the lock to be released.</param>
    /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
    public static void AcquireWithTransaction(PostgresAdvisoryLockKey key, IDbTransaction transaction, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(state => AcquireWithTransactionAsyncInternal(state.key, state.transaction, state.timeout, state.cancellationToken), (key, transaction, timeout, cancellationToken));

    /// <summary>
    /// Attempts to acquire a transaction-scoped advisory lock asynchronously with an externally owned transaction. Usage: 
    /// <code>
    ///     var transaction = /* create a DB transaction */ 
    /// 
    ///     var isLockAcquired = await myLock.TryAcquireWithTransactionAsync(..., transaction, ...)
    ///
    ///     if (isLockAcquired != null) 
    ///     { 
    ///         /* we have the lock! */ 
    ///         
    ///         // Commit or Rollback the transaction, which in turn will release the lock
    ///     }
    /// </code>
    /// 
    /// NOTE: The owner of the transaction is the responsible party for it - the owner must commit or rollback the transaction in order to release the acquired lock.
    /// </summary>
    /// <param name="key">The postgres advisory lock key which will be used to acquire the lock.</param>
    /// <param name="transaction">The externally owned transaction which will be used to acquire the lock. The owner of the transaction must commit or rollback it for the lock to be released.</param>
    /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to 0.</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
    /// <returns>Whether the lock has been acquired</returns>
    public static ValueTask<bool> TryAcquireWithTransactionAsync(PostgresAdvisoryLockKey key, IDbTransaction transaction, TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        TryAcquireWithTransactionAsyncInternal(key, transaction, timeout, cancellationToken);

    /// <summary>
    /// Acquires a transaction-scoped advisory lock asynchronously, failing with <see cref="TimeoutException"/> if the attempt times out. Usage: 
    /// <code>
    ///     var transaction = /* create a DB transaction */ 
    /// 
    ///     await myLock.AcquireWithTransaction(..., transaction, ...)
    ///
    ///     /* we have the lock! */ 
    ///         
    ///     // Commit or Rollback the transaction, which in turn will release the lock
    /// </code>
    /// 
    /// NOTE: The owner of the transaction is the responsible party for it - the owner must commit or rollback the transaction in order to release the acquired lock.
    /// </summary>
    /// <param name="key">The postgres advisory lock key which will be used to acquire the lock.</param>
    /// <param name="transaction">The externally owned transaction which will be used to acquire the lock. The owner of the transaction must commit or rollback it for the lock to be released.</param>
    /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
    public static ValueTask AcquireWithTransactionAsync(PostgresAdvisoryLockKey key, IDbTransaction transaction, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        AcquireWithTransactionAsyncInternal(key, transaction, timeout, cancellationToken);

    internal static ValueTask<bool> TryAcquireWithTransactionAsyncInternal(PostgresAdvisoryLockKey key, IDbTransaction transaction, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (key == null) { throw new ArgumentNullException(nameof(key)); }
        if (transaction == null) { throw new ArgumentNullException(nameof(transaction)); }

        return TryAcquireAsync();

        async ValueTask<bool> TryAcquireAsync()
        {
            var connection = new PostgresDatabaseConnection(transaction);

            await using (connection.ConfigureAwait(false))
            {
                var lockAcquiredCookie = await PostgresAdvisoryLock.ExclusiveLock.TryAcquireAsync(connection, key.ToString(), timeout, cancellationToken).ConfigureAwait(false);

                return lockAcquiredCookie != null;
            }
        }
    }

    internal static ValueTask AcquireWithTransactionAsyncInternal(PostgresAdvisoryLockKey key, IDbTransaction transaction, TimeSpan? timeout, CancellationToken cancellationToken)
    {
        if (key == null) { throw new ArgumentNullException(nameof(key)); }
        if (transaction == null) { throw new ArgumentNullException(nameof(transaction)); }

        return AcquireAsync();

        async ValueTask AcquireAsync()
        {
            var connection = new PostgresDatabaseConnection(transaction);

            await using (connection.ConfigureAwait(false))
            {
                await PostgresAdvisoryLock.ExclusiveLock.TryAcquireAsync(connection, key.ToString(), timeout, cancellationToken).ThrowTimeoutIfNull().ConfigureAwait(false);
            }
        }
    }
}
