using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.MySql
{
    /// <summary>
    /// Implements <see cref="IDbSynchronizationStrategy{TLockCookie}"/> using user-level locking functions. See
    /// https://dev.mysql.com/doc/refman/8.0/en/locking-functions.html
    /// </summary>
    internal class MySqlUserLock : IDbSynchronizationStrategy<object>
    {
        // matches SqlApplicationLock
        private const int AlreadyHeldReturnCode = 103;
        // see behavior documented at https://mariadb.com/kb/en/get_lock/ for when GET_LOCK returns NULL
        private const int GetLockErrorReturnCode = 104;

        private static readonly object Cookie = new();

        public bool IsUpgradeable => false;

        public async ValueTask ReleaseAsync(DatabaseConnection connection, string resourceName, object lockCookie)
        {
            using var command = connection.CreateCommand();
            command.SetCommandText("DO RELEASE_LOCK(@name)");
            command.AddParameter("name", resourceName);
            await command.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
        }

        public async ValueTask<object?> TryAcquireAsync(DatabaseConnection connection, string resourceName, TimeoutValue timeout, CancellationToken cancellationToken)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.SetCommandText($"SELECT CASE WHEN IS_USED_LOCK(@name) = CONNECTION_ID() THEN {AlreadyHeldReturnCode} ELSE IFNULL(GET_LOCK(@name, @timeoutSeconds), {GetLockErrorReturnCode}) END");
                command.AddParameter("name", resourceName);
                // Note: -1 works for MySQL but not for MariaDB (https://stackoverflow.com/questions/49792089/set-infinite-timeout-get-lock-in-mariadb/49809919)
                command.AddParameter("timeoutSeconds", timeout.IsInfinite ? 0xFFFFFFFF : timeout.InSeconds);

                // Convert required because the value comes back as int on MariaDB and long on MySQL
                var acquireCommandResult = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
                switch (acquireCommandResult)
                {
                    case 0: // timeout
                        return null;
                    case 1: // success
                        return Cookie;
                    case AlreadyHeldReturnCode:
                        if (timeout.IsZero) { return null; }
                        if (timeout.IsInfinite) { throw new DeadlockException("Attempted to acquire a lock that is already held on the same connection"); }
                        await SyncViaAsync.Delay(timeout, cancellationToken).ConfigureAwait(false);
                        return null;
                    case GetLockErrorReturnCode:
                        cancellationToken.ThrowIfCancellationRequested(); // this error can also indicate cancellation in MariaDB
                        throw new InvalidOperationException("An error occurred such as running out of memory on the thread or a mysqladmin kill when trying to acquire the lock");
                    default:
                        throw new InvalidOperationException($"Unexpected return code {acquireCommandResult}");
                }
            }
            catch (MySqlException ex)
                // from https://dev.mysql.com/doc/mysql-errors/8.0/en/server-error-reference.html#error_er_user_lock_deadlock
                when ((ex.Number == 3058 && ex.SqlState == "HY000")
                    // from https://mariadb.com/kb/en/mariadb-error-codes/
                    || (ex.Number == 1213 && ex.SqlState == "40001"))
            {
                throw new DeadlockException($"The request for the distributed lock failed with deadlock exit code {ex.Number}", ex);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // If the command is canceled, I believe there's a slim chance that acquisition just completed before the cancellation went through.
                // In that case, I'm pretty sure it won't be rolled back. Therefore, to be safe we issue a release
                await this.ReleaseAsync(connection, resourceName, Cookie).ConfigureAwait(false);
                throw;
            }
        }
    }
}
