using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using Npgsql;
using System.Data;
using System.Text;

namespace Medallion.Threading.Postgres;

/// <summary>
/// Implements <see cref="IDbSynchronizationStrategy{TLockCookie}"/> using advisory locking functions
/// (see https://www.postgresql.org/docs/12/functions-admin.html#FUNCTIONS-ADVISORY-LOCKS)
/// </summary>
internal class PostgresAdvisoryLock : IDbSynchronizationStrategy<object>
{
    private static readonly object Cookie = new();

    public static readonly PostgresAdvisoryLock ExclusiveLock = new(isShared: false),
        SharedLock = new(isShared: true);

    private readonly bool _isShared;

    private PostgresAdvisoryLock(bool isShared)
    {
        this._isShared = isShared;
    }

    /// <summary>
    /// Advisory locks don't natively support upgradeable 
    /// </summary>
    public bool IsUpgradeable => false;

    public async ValueTask<object?> TryAcquireAsync(DatabaseConnection connection, string resourceName, TimeoutValue timeout, CancellationToken cancellationToken)
    {
        const string SavePointName = "medallion_threading_postgres_advisory_lock_acquire";

        PostgresAdvisoryLockKey key = new(resourceName);

        if (connection.IsExernallyOwned 
            && await this.IsHoldingLockAsync(connection, key, cancellationToken).ConfigureAwait(false))
        {
            if (timeout.IsZero) { return null; }
            if (timeout.IsInfinite) { throw new DeadlockException("Attempted to acquire a lock that is already held on the same connection"); }
            await SyncViaAsync.Delay(timeout, cancellationToken).ConfigureAwait(false);
            return null;
        }

        // Only in the case where we will try to acquire a transaction-scoped lock, we will define a save point, but we won't be able to roll it back
        // in case of a successful lock acquisition becuase the lock will be released. Therefore, in such cases, we capture the timeout settings values before
        // we set a save point, and then we try to restore the values after the attempt to acquire the lock.
        // NOTE: the save point functionality can't be removed in favor of capturing and restoring the values for all cases.
        // When an error occurs while attempting to acquire the lock, the transaction is aborted and we can't run any other query, unless either
        // the transaction or the save point are rolled back.
        var capturedTimeoutSettings = await CaptureTimeoutSettingsIfNeededAsync(connection, cancellationToken).ConfigureAwait(false);

        // Our acquire command will use SET LOCAL to set up statement timeouts. This lasts until the end
        // of the current transaction instead of just the current batch if we're in a transaction. To make sure
        // we don't leak those settings, in the case of a transaction, we first set up a save point which we can
        // later roll back (only in cases where we don't acquire a transaction scoped lock - taking the settings changes with it but NOT the lock).
        // Because we can't confidently roll back a save point without knowing that it has been set up, we start the save point in its own
        // query before we try-catch.
        var needsSavePoint = await ShouldDefineSavePoint(connection).ConfigureAwait(false);

        if (needsSavePoint)
        {
            using var setSavePointCommand = connection.CreateCommand();
            setSavePointCommand.SetCommandText("SAVEPOINT " + SavePointName);
            await setSavePointCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        using var acquireCommand = this.CreateAcquireCommand(connection, key, timeout);

        object? acquireCommandResult;
        try
        {
            acquireCommandResult = await acquireCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await RollBackTransactionTimeoutVariablesIfNeededAsync(acquired: false).ConfigureAwait(false);

            await RestoreTimeoutSettingsIfNeededAsync(capturedTimeoutSettings, connection).ConfigureAwait(false);

            if (ex is PostgresException postgresException)
            {
                switch (postgresException.SqlState)
                {
                    // lock_timeout error code from https://www.postgresql.org/docs/16/errcodes-appendix.html
                    case "55P03":
                        // Even though we hit a lock timeout, an underlying race condition in Postgres means that we might actually
                        // have acquired the lock right before timing out. To account for this, we simply re-check whether we are
                        // holding the lock to determine the final return value. See https://github.com/madelson/DistributedLock/issues/147
                        // and https://www.postgresql.org/message-id/63573.1668271677%40sss.pgh.pa.us for more details.
                        // NOTE: we use CancellationToken.None for this check because if we ARE holding the lock it would be invalid to abort.
                        return await this.IsHoldingLockAsync(connection, key, CancellationToken.None).ConfigureAwait(false)
                            ? Cookie
                            : null;
                    // deadlock_detected error code from https://www.postgresql.org/docs/16/errcodes-appendix.html
                    case "40P01":
                        throw new DeadlockException($"The request for the distributed lock failed with exit code '{postgresException.SqlState}' (deadlock_detected)", ex);
                }
            }

            if (ex is OperationCanceledException 
                && cancellationToken.IsCancellationRequested
                // There's no way to explicitly release transaction-scoped locks other than a rollback; in our case
                // RollBackTransactionTimeoutVariablesIfNeededAsync will have already released by rolling back the savepoint.
                // Furthermore the caller will proceed to dispose the transaction.
                && !UseTransactionScopedLock(connection))
            {
                // if we bailed in the middle of an acquire, make sure we didn't leave a lock behind
                await this.ReleaseAsync(connection, key, isTry: true).ConfigureAwait(false);
            }

            throw;
        }

        var acquired = acquireCommandResult switch
        {
            DBNull _ => true, // indicates we called pg_advisory_lock and not pg_try_advisory_lock
            null => true, // Npgsql 8 returns null instead of DBNull
            false => false,
            true => true,
            _ => default(bool?)
        };

        await RollBackTransactionTimeoutVariablesIfNeededAsync(acquired: acquired == true).ConfigureAwait(false);

        await RestoreTimeoutSettingsIfNeededAsync(capturedTimeoutSettings, connection).ConfigureAwait(false);

        return acquired switch
        {
            false => null,
            true => Cookie,
            null => throw new InvalidOperationException($"Unexpected value '{acquireCommandResult}' from acquire command")
        };

        async ValueTask RollBackTransactionTimeoutVariablesIfNeededAsync(bool acquired)
        {
            if (needsSavePoint
                // For transaction scoped locks, we can't roll back the save point on success because that will roll back our hold on the lock.
                && !(acquired && UseTransactionScopedLock(connection)))
            {
                // attempt to clear the timeout variables we set
                using var rollBackSavePointCommand = connection.CreateCommand();
                rollBackSavePointCommand.SetCommandText("ROLLBACK TO SAVEPOINT " + SavePointName);
                await rollBackSavePointCommand.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private async Task<bool> IsHoldingLockAsync(DatabaseConnection connection, PostgresAdvisoryLockKey key, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.SetCommandText($@"
                SELECT COUNT(*) 
                FROM pg_catalog.pg_locks l
                JOIN pg_catalog.pg_database d
                    ON d.oid = l.database
                WHERE l.locktype = 'advisory' 
                    AND {AddPGLocksFilterParametersAndGetFilterExpression(command, key)} 
                    AND l.pid = pg_catalog.pg_backend_pid() 
                    AND d.datname = pg_catalog.current_database()"
        );
        return (long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))! != 0;
    }

    private DatabaseCommand CreateAcquireCommand(DatabaseConnection connection, PostgresAdvisoryLockKey key, TimeoutValue timeout)
    {
        var command = connection.CreateCommand();

        var commandText = new StringBuilder();

        // We set the statement_timeout to 0 (inf) because we want everything to be driven by the lock_timeout.
        commandText.AppendLine("SET LOCAL statement_timeout = 0;");
        // We set the lock timeout to our timeout, with the exception that if our timeout is zero we set it to inf because
        // we'll be using the pg_try_advisory_lock function which doesn't block in that case.
        commandText.AppendLine($"SET LOCAL lock_timeout = {(timeout.IsZero || timeout.IsInfinite ? 0 : timeout.InMilliseconds)};");

        commandText.Append("SELECT ");
        var isTry = timeout.IsZero;
        commandText.Append("pg_catalog.pg");
        if (isTry) { commandText.Append("_try"); }
        commandText.Append("_advisory");
        if (UseTransactionScopedLock(connection)) { commandText.Append("_xact"); }
        commandText.Append("_lock");
        if (this._isShared) { commandText.Append("_shared"); }
        commandText.Append('(').Append(AddKeyParametersAndGetKeyArguments(command, key)).Append(')')
            .Append(" AS result");

        command.SetCommandText(commandText.ToString());
        command.SetTimeout(timeout);

        return command;
    }

    private static async ValueTask<CapturedTimeoutSettings?> CaptureTimeoutSettingsIfNeededAsync(DatabaseConnection connection, CancellationToken cancellationToken)
    {
        var shouldCaptureTimeoutSettings = UseTransactionScopedLock(connection);

        // Return null in case we won't try to acquire a transaction-scoped lock.
        if (!shouldCaptureTimeoutSettings) { return null; }

        var statementTimeout = await GetCurrentSetting("statement_timeout", connection, cancellationToken).ConfigureAwait(false);
        var lockTimeout = await GetCurrentSetting("lock_timeout", connection, cancellationToken).ConfigureAwait(false);

        var capturedTimeoutSettings = new CapturedTimeoutSettings(statementTimeout!, lockTimeout!);

        return capturedTimeoutSettings;
    }

    private static async ValueTask<string?> GetCurrentSetting(string settingName, DatabaseConnection connection, CancellationToken cancellationToken)
    {
        using var getCurrentSettingCommand = connection.CreateCommand();

        getCurrentSettingCommand.SetCommandText($"SELECT current_setting('{settingName}', 'true') AS {settingName};");

        return (string?) await getCurrentSettingCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<bool> ShouldDefineSavePoint(DatabaseConnection connection)
    {
        // If the connection is internally-owned, we only define a save point if a transaction has been opened.
        if (!connection.IsExernallyOwned) { return connection.HasTransaction; }

        // If the connection is externally-owned with an established transaction,
        // it means that the connection came through the transactional locking APIs (see PostgresDistributedLock.Extensions class).
        if (connection.HasTransaction) { return true; }

        // The externally-owned connection might still be part of a transaction that we can't see.
        // This can only be the case if the externally-owned connection didn't came through the transactional locking APIs (see PostgresDistributedLock.Extensions class).
        // In that case, the only real way to detect the transaction is to begin a new one.
        try
        {
            await connection.BeginTransactionAsync().ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // If we reached this point, it means the externally-owned connection has a transaction, therefore we need to define a save point.
            return true;
        }

        await connection.DisposeTransactionAsync().ConfigureAwait(false);

        // If we reached this point, it means the externally-owned connection has no transaction, therefore we can't define a save point.
        return false;
    }

    private static async ValueTask RestoreTimeoutSettingsIfNeededAsync(CapturedTimeoutSettings? settings, DatabaseConnection connection)
    {
        // Settings is expected to be null in case we didn't try to acquire a transaction-scoped lock.
        if (settings is null) { return; }

        using var restoreTimeoutSettingsCommand = connection.CreateCommand();

        var commandText = new StringBuilder();

        commandText.AppendLine($"SET LOCAL statement_timeout = {settings.Value.StatementTimeout};");
        commandText.AppendLine($"SET LOCAL lock_timeout = {settings.Value.LockTimeout};");
        
        restoreTimeoutSettingsCommand.SetCommandText(commandText.ToString());

        await restoreTimeoutSettingsCommand.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
    }

    public ValueTask ReleaseAsync(DatabaseConnection connection, string resourceName, object lockCookie) =>
        this.ReleaseAsync(connection, new PostgresAdvisoryLockKey(resourceName), isTry: false);

    private async ValueTask ReleaseAsync(DatabaseConnection connection, PostgresAdvisoryLockKey key, bool isTry)
    {
        Invariant.Require(!UseTransactionScopedLock(connection));

        using var command = connection.CreateCommand();
        command.SetCommandText($"SELECT pg_catalog.pg_advisory_unlock{(this._isShared ? "_shared" : string.Empty)}({AddKeyParametersAndGetKeyArguments(command, key)})");
        var result = (bool)(await command.ExecuteScalarAsync(CancellationToken.None).ConfigureAwait(false))!;
        if (!isTry && !result)
        {
            throw new InvalidOperationException("Attempted to release a lock that was not held");
        }
    }

    private static string AddKeyParametersAndGetKeyArguments(DatabaseCommand command, PostgresAdvisoryLockKey key)
    {
        if (key.HasSingleKey)
        {
            command.AddParameter("key", key.Key, DbType.Int64);
            return "@key";
        }
        else
        {
            var (key1, key2) = key.Keys;
            command.AddParameter("key1", key1, DbType.Int32);
            command.AddParameter("key2", key2, DbType.Int32);
            return "@key1, @key2";
        }
    }

    private static bool UseTransactionScopedLock(DatabaseConnection connection) =>
        // Transaction-scoped locking is supported on internally-owned connections and externally-owned connections which explicitly have a transaction
        // (meaning that the external connection came through the transactional locking APIs, see PostgresDistributedLock.Extensions class).
        connection.HasTransaction;

    private static string AddPGLocksFilterParametersAndGetFilterExpression(DatabaseCommand command, PostgresAdvisoryLockKey key)
    {
        // From https://www.postgresql.org/docs/12/view-pg-locks.html
        // Advisory locks can be acquired on keys consisting of either a single bigint value or two integer values. 
        // A bigint key is displayed with its high-order half in the classid column, its low-order half in the objid column, 
        // and objsubid equal to 1. The original bigint value can be reassembled with the expression (classid::bigint << 32) | objid::bigint. 
        // Integer keys are displayed with the first key in the classid column, the second key in the objid column, and objsubid equal to 2.

        string classIdParameter, objIdParameter, objSubId;
        if (key.HasSingleKey)
        {
            // since Postgres seems to lack unchecked int conversions, it is simpler to just generate extra
            // parameters to carry the split key info in this case
            var (keyUpper32, keyLower32) = key.Keys;
            command.AddParameter(classIdParameter = "keyUpper32", keyUpper32, DbType.Int32);
            command.AddParameter(objIdParameter = "keyLower32", keyLower32, DbType.Int32);
            objSubId = "1";
        }
        else
        {
            AddKeyParametersAndGetKeyArguments(command, key);
            classIdParameter = "key1";
            objIdParameter = "key2";
            objSubId = "2";
        }

        return $"(l.classid = @{classIdParameter} AND l.objid = @{objIdParameter} AND l.objsubid = {objSubId})";
    }

    private readonly struct CapturedTimeoutSettings
    {
        public CapturedTimeoutSettings(string statementTimeout, string lockTimeout)
        {
            this.StatementTimeout = ParsePostgresTimeout(statementTimeout);
            this.LockTimeout = ParsePostgresTimeout(lockTimeout);
        }

        public int StatementTimeout { get; }

        public int LockTimeout { get; }

        private static int ParsePostgresTimeout(string timeout)
        {
            if (timeout == "0") { return 0; } // This will be the case if the timeout is disabled.

            // In any other case we need to extract the timeout from the string, since Postgres returns timeouts with their unit attached, e.g. "5000ms".
            var timeoutOnlyDigits = string.Empty;

            for (var i = 0; i < timeout.Length; ++i)
            {
                if (char.IsDigit(timeout[i])) 
                { 
                    timeoutOnlyDigits += timeout[i]; 
                }
            }

            return int.Parse(timeoutOnlyDigits);
        }
    }
}
