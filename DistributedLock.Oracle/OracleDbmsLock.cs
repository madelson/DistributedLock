using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Oracle
{
    /// <summary>
    /// Implements <see cref="IDbSynchronizationStrategy{TLockCookie}"/> using Oracle's DBMS_LOCK package
    /// </summary>
    internal class OracleDbmsLock : IDbSynchronizationStrategy<object>
    {
        // https://docs.oracle.com/cd/B19306_01/appdev.102/b14258/d_lock.htm#i1002309
        private const int MaxWaitSeconds = 32767;
        private const int MaxTimeoutSeconds = MaxWaitSeconds - 1;

        private static readonly object Cookie = new();

        public bool IsUpgradeable => false; // TODO revisit

        public async ValueTask ReleaseAsync(DatabaseConnection connection, string resourceName, object lockCookie)
        {
            using var command = connection.CreateCommand();
            command.SetCommandText(@"
                DECLARE
                    lockHandle VARCHAR2(128);
                BEGIN
                    SYS.DBMS_LOCK.ALLOCATE_UNIQUE(:lockName, lockHandle);
                    :returnValue := SYS.DBMS_LOCK.RELEASE(lockHandle);
                END;"
            );
            // note: parameters bind by position by default!
            command.AddParameter("lockName", resourceName);
            var returnValueParameter = command.AddParameter("returnValue", type: DbType.Int32, direction: ParameterDirection.Output);
            await command.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);

            var returnValue = (int)returnValueParameter.Value;
            if (returnValue != 0)
            {
                // we don't enumerate the error codes here because release shouldn't ever fail unless the user really messes things up
                throw new InvalidOperationException($"SYS.DBMS_LOCK.RELEASE returned error code {returnValue}");
            }
        }

        public async ValueTask<object?> TryAcquireAsync(DatabaseConnection connection, string resourceName, TimeoutValue timeout, CancellationToken cancellationToken)
        {
            using var command = connection.CreateCommand();
            // TODO revisit mode, release_on_commit
            command.SetCommandText(@"
                DECLARE
                    lockHandle VARCHAR2(128);
                BEGIN
                    SYS.DBMS_LOCK.ALLOCATE_UNIQUE(:lockName, lockHandle);
                    :returnValue := SYS.DBMS_LOCK.REQUEST(lockhandle => lockHandle, lockmode => SYS.DBMS_LOCK.X_MODE, timeout => :timeout, release_on_commit => FALSE);
                END;"
            );
            // note: parameters bind by position by default!
            command.AddParameter("lockName", resourceName);
            var returnValueParameter = command.AddParameter("returnValue", type: DbType.Int32, direction: ParameterDirection.Output);
            command.AddParameter(
                "timeout",
                timeout.IsInfinite ? MaxWaitSeconds
                    // we could support longer timeouts via looping lock requests, but this doesn't feel particularly valuable and isn't a true longer wait
                    // since by looping you fall out of the wait queue
                    : timeout.TimeSpan.TotalSeconds > MaxTimeoutSeconds ? throw new ArgumentOutOfRangeException($"Requested non-infinite timeout value '{timeout}' is longer than Oracle's allowed max of '{TimeSpan.FromSeconds(MaxTimeoutSeconds)}'")
                    : timeout.TimeSpan.TotalSeconds
            );
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            var returnValue = (int)returnValueParameter.Value;
            return returnValue switch
            {
                0 => Cookie, // success
                1 => null, // timeout
                2 => throw new DeadlockException(GetErrorMessage("deadlock")),
                3 => throw new InvalidOperationException(GetErrorMessage("parameter error")),
                4 => timeout.IsZero ? null
                    : timeout.IsInfinite ? throw new DeadlockException("Attempted to acquire a lock that is already held on the same connection")
                    : await WaitThenReturnNullAsync().ConfigureAwait(false),
                5 => throw new InvalidOperationException(GetErrorMessage("illegal lock handle")),
                _ => throw new InvalidOperationException(GetErrorMessage("unknown error code")),
            };

            string GetErrorMessage(string description) =>
                $"SYS.DBMS_LOCK.REQUEST returned error code {returnValue} ({description})";

            async ValueTask<object?> WaitThenReturnNullAsync()
            {
                await SyncViaAsync.Delay(timeout, cancellationToken).ConfigureAwait(false);
                return null;
            }
        }
    }
}
