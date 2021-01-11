using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Postgres
{
    /// <summary>
    /// Implements <see cref="IDbSynchronizationStrategy{TLockCookie}"/> using advisory locking functions
    /// (see https://www.postgresql.org/docs/12/functions-admin.html#FUNCTIONS-ADVISORY-LOCKS)
    /// </summary>
    internal class PostgresAdvisoryLock : IDbSynchronizationStrategy<object>
    {
        // matches SqlApplicationLock
        private const int AlreadyHeldReturnCode = 103;

        private static readonly object Cookie = new object();

        public static readonly PostgresAdvisoryLock ExclusiveLock = new PostgresAdvisoryLock(isShared: false),
            SharedLock = new PostgresAdvisoryLock(isShared: true);

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

            var key = new PostgresAdvisoryLockKey(resourceName);

            var hasTransaction = await HasTransactionAsync(connection).ConfigureAwait(false);
            if (hasTransaction)
            {
                // Our acquire command will use SET LOCAL to set up statement timeouts. This lasts until the end
                // of the current transaction instead of just the current batch if we're in a transaction. To make sure
                // we don't leak those settings, in the case of a transaction we first set up a save point which we can
                // later roll back (taking the settings changes with it but NOT the lock). Because we can't confidently
                // roll back a save point without knowing that it has been set up, we start the save point in its own
                // query before we try-catch
                using var setSavePointCommand = connection.CreateCommand();
                setSavePointCommand.SetCommandText("SAVEPOINT " + SavePointName);
                await setSavePointCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            using var acquireCommand = this.CreateAcquireCommand(connection, key, timeout);

            int acquireCommandResult;
            try
            {
                acquireCommandResult = (int)await acquireCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await RollBackTransactionTimeoutVariablesIfNeededAsync().ConfigureAwait(false);

                if (ex is PostgresException postgresException)
                {
                    switch (postgresException.SqlState)
                    {
                        // lock_timeout error code from https://www.postgresql.org/docs/10/errcodes-appendix.html
                        case "55P03":
                            return null;
                        // deadlock_detected error code from https://www.postgresql.org/docs/10/errcodes-appendix.html
                        case "40P01":
                            throw new DeadlockException($"The request for the distributed lock failed with exit code '{postgresException.SqlState}' (deadlock_detected)", ex);
                    }
                }

                if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
                {
                    // if we bailed in the middle of an acquire, make sure we didn't leave a lock behind
                    await this.ReleaseAsync(connection, key, isTry: true).ConfigureAwait(false);
                }

                throw;
            }

            await RollBackTransactionTimeoutVariablesIfNeededAsync().ConfigureAwait(false);

            switch (acquireCommandResult)
            {
                case 0: return null;
                case 1: return Cookie;
                case AlreadyHeldReturnCode:
                    if (timeout.IsZero) { return null; }
                    if (timeout.IsInfinite) { throw new DeadlockException("Attempted to acquire a lock that is already held on the same connection"); }
                    await SyncViaAsync.Delay(timeout, cancellationToken).ConfigureAwait(false);
                    return null;
                default:
                    throw new InvalidOperationException($"Unexpected return code {acquireCommandResult}");
            }

            async ValueTask RollBackTransactionTimeoutVariablesIfNeededAsync()
            {
                if (hasTransaction)
                {
                    // attempt to clear the timeout variables we set
                    using var rollBackSavePointCommand = connection.CreateCommand();
                    rollBackSavePointCommand.SetCommandText("ROLLBACK TO SAVEPOINT " + SavePointName);
                    await rollBackSavePointCommand.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        private DatabaseCommand CreateAcquireCommand(DatabaseConnection connection, PostgresAdvisoryLockKey key, TimeoutValue timeout)
        {
            var command = connection.CreateCommand();

            var commandText = new StringBuilder();

            commandText.AppendLine("SET LOCAL statement_timeout = 0;");
            commandText.AppendLine($"SET LOCAL lock_timeout = {(timeout.IsZero || timeout.IsInfinite ? 0 : timeout.InMilliseconds)};");

            if (connection.IsExernallyOwned)
            {
                commandText.Append($@"
                    SELECT 
                        CASE WHEN EXISTS(
                            SELECT * 
                            FROM pg_catalog.pg_locks l
                            JOIN pg_catalog.pg_database d
                                ON d.oid = l.database
                            WHERE l.locktype = 'advisory' 
                                AND {AddPGLocksFilterParametersAndGetFilterExpression(command, key)} 
                                AND l.pid = pg_catalog.pg_backend_pid() 
                                AND d.datname = pg_catalog.current_database()
                        ) 
                            THEN {AlreadyHeldReturnCode}
                        ELSE
                            "
                );
                AppendAcquireFunctionCall();
                commandText.AppendLine().Append("END");
            }
            else
            {
                commandText.Append("SELECT ");
                AppendAcquireFunctionCall();
            }
            commandText.Append(" AS result");

            command.SetCommandText(commandText.ToString());
            command.SetTimeout(timeout);

            return command;

            void AppendAcquireFunctionCall()
            {
                // creates an expression like
                // pg_try_advisory_lock(@key1, @key2)::int
                // OR (SELECT 1 FROM (SELECT pg_advisory_lock(@key)) f)
                var isTry = timeout.IsZero;
                if (!isTry) { commandText.Append("(SELECT 1 FROM (SELECT "); }
                commandText.Append("pg_catalog.pg");
                if (isTry) { commandText.Append("_try"); }
                commandText.Append("_advisory");
                commandText.Append("_lock");
                if (this._isShared) { commandText.Append("_shared"); }
                commandText.Append('(').Append(AddKeyParametersAndGetKeyArguments(command, key)).Append(')');
                if (isTry) { commandText.Append("::int"); }
                else { commandText.Append(") f)"); }
            }
        }

        private static async ValueTask<bool> HasTransactionAsync(DatabaseConnection connection)
        {
            if (connection.HasTransaction) { return true; }
            if (!connection.IsExernallyOwned) { return false; }

            // If the connection is externally owned, then it might be part of a transaction that we can't
            // see. In that case, the only real way to detect it is to begin a new one
            try
            {
                await connection.BeginTransactionAsync().ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                return true;
            }

            await connection.DisposeTransactionAsync().ConfigureAwait(false);
            return false;
        }

        public ValueTask ReleaseAsync(DatabaseConnection connection, string resourceName, object lockCookie) =>
            this.ReleaseAsync(connection, new PostgresAdvisoryLockKey(resourceName), isTry: false);

        private async ValueTask ReleaseAsync(DatabaseConnection connection, PostgresAdvisoryLockKey key, bool isTry)
        {
            using var command = connection.CreateCommand();
            command.SetCommandText($"SELECT pg_catalog.pg_advisory_unlock{(this._isShared ? "_shared" : string.Empty)}({AddKeyParametersAndGetKeyArguments(command, key)})");
            var result = (bool)await command.ExecuteScalarAsync(CancellationToken.None).ConfigureAwait(false);
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
                classIdParameter = "key1";
                objIdParameter = "key2";
                objSubId = "2";
            }

            return $"(l.classid = @{classIdParameter} AND l.objid = @{objIdParameter} AND l.objsubid = {objSubId})";
        }
    }
}
