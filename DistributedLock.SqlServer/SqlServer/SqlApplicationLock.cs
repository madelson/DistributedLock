using Medallion.Threading.Data;
using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.SqlServer
{
    /// <summary>
    /// Implements <see cref="IDbSynchronizationStrategy{TLockCookie}"/> using sp_getapplock
    /// </summary>
    internal sealed class SqlApplicationLock : IDbSynchronizationStrategy<object>
    {
        public const int TimeoutExitCode = -1;

        public static readonly SqlApplicationLock SharedLock = new SqlApplicationLock(Mode.Shared),
            UpdateLock = new SqlApplicationLock(Mode.Update),
            ExclusiveLock = new SqlApplicationLock(Mode.Exclusive);

        private static readonly object Cookie = new object();
        private readonly Mode mode;

        private SqlApplicationLock(Mode mode)
        {
            this.mode = mode;
        }

        bool IDbSynchronizationStrategy<object>.IsUpgradeable => this.mode == Mode.Update;

        async ValueTask<object?> IDbSynchronizationStrategy<object>.TryAcquireAsync(
            DatabaseConnection connection, 
            string resourceName, 
            TimeoutValue timeout, 
            CancellationToken cancellationToken)
        {
            try
            {
                return await ExecuteAcquireCommandAsync(connection, resourceName, timeout, this.mode, cancellationToken).ConfigureAwait(false) ? Cookie : null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // If the command is canceled, I believe there's a slim chance that acquisition just completed before the cancellation went through.
                // In that case, I'm pretty sure it won't be rolled back. Therefore, to be safe we issue a try-release 
                await ExecuteReleaseCommandAsync(connection, resourceName, isTry: true).ConfigureAwait(false);
                throw;
            }
        }

        ValueTask IDbSynchronizationStrategy<object>.ReleaseAsync(DatabaseConnection connection, string resourceName, object lockCookie) =>
            ExecuteReleaseCommandAsync(connection, resourceName, isTry: false);

        private static async Task<bool> ExecuteAcquireCommandAsync(DatabaseConnection connection, string lockName, TimeoutValue timeout, Mode mode, CancellationToken cancellationToken)
        {
            using var command = CreateAcquireCommand(connection, lockName, timeout, mode, out var returnValue);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return ParseExitCode((int)returnValue.Value);
        }

        private static async ValueTask ExecuteReleaseCommandAsync(DatabaseConnection connection, string lockName, bool isTry)
        {
            using var command = CreateReleaseCommand(connection, lockName, isTry, out var returnValue);
            await command.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
            ParseExitCode((int)returnValue.Value);
        }

        private static DatabaseCommand CreateAcquireCommand(
            DatabaseConnection connection, 
            string lockName, 
            TimeoutValue timeout, 
            Mode mode,
            out IDbDataParameter returnValue)
        {
            // todo when we're checking recursion this should not be a stored proc

            var command = connection.CreateCommand();
            command.SetCommandText("dbo.sp_getapplock");
            command.SetCommandType(CommandType.StoredProcedure);
            command.SetTimeout(timeout);

            command.AddParameter("Resource", lockName);
            command.AddParameter("LockMode", GetModeString(mode));
            command.AddParameter("LockOwner", connection.HasTransaction ? "Transaction" : "Session");
            command.AddParameter("LockTimeout", timeout.InMilliseconds);

            returnValue = command.AddParameter(type: DbType.Int32, direction: ParameterDirection.ReturnValue);

            return command;
        }

        private static DatabaseCommand CreateReleaseCommand(DatabaseConnection connection, string lockName, bool isTry, out IDbDataParameter returnValue)
        {
            var command = connection.CreateCommand();
            if (isTry)
            {
                command.SetCommandText(
                    @"IF APPLOCK_MODE('public', @Resource, @LockOwner) != 'NoLock'
                        EXEC @Result = dbo.sp_releaseapplock @Resource, @LockOwner
                      ELSE
                        SET @Result = 0"
                );
            }
            else
            {
                command.SetCommandText("dbo.sp_releaseapplock");
                command.SetCommandType(CommandType.StoredProcedure);
            }

            command.AddParameter("Resource", lockName);
            command.AddParameter("LockOwner", connection.HasTransaction ? "Transaction" : "Session");

            if (isTry)
            {
                returnValue = command.AddParameter("Result", type: DbType.Int32, direction: ParameterDirection.Output);
            }
            else
            {
                returnValue = command.AddParameter(type: DbType.Int32, direction: ParameterDirection.ReturnValue);
            } 
            
            return command;
        }

        public static bool ParseExitCode(int exitCode)
        {
            // sp_getapplock exit codes documented at
            // https://msdn.microsoft.com/en-us/library/ms189823.aspx

            switch (exitCode)
            {
                case 0:
                case 1:
                    return true;

                case TimeoutExitCode:
                    return false;

                case -2: // canceled
                    throw new OperationCanceledException(GetErrorMessage(exitCode, "canceled"));
                case -3: // deadlock
                    throw new DeadlockException(GetErrorMessage(exitCode, "deadlock"));
                case -999: // parameter / unknown
                    throw new ArgumentException(GetErrorMessage(exitCode, "parameter validation or other error"));

                default:
                    if (exitCode <= 0)
                        throw new InvalidOperationException(GetErrorMessage(exitCode, "unknown"));
                    return true; // unknown success code
            }
        }

        private static string GetErrorMessage(int exitCode, string type) => $"The request for the distributed lock failed with exit code {exitCode} ({type})";

        private static string GetModeString(Mode mode) => mode switch
        {
            Mode.Shared => "Shared",
            Mode.Update => "Update",
            Mode.Exclusive => "Exclusive",
            _ => throw new ArgumentException(nameof(mode)),
        };

        private enum Mode
        {
            Shared,
            Update,
            Exclusive,
        }
    }
}
