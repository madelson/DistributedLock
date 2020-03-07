using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    internal sealed class SqlApplicationLock : ISqlSynchronizationStrategy<object>
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

        bool ISqlSynchronizationStrategy<object>.IsUpgradeable => this.mode == Mode.Update;

        async ValueTask<object?> ISqlSynchronizationStrategy<object>.TryAcquireAsync(
            ConnectionOrTransaction connectionOrTransaction, 
            string resourceName, 
            TimeoutValue timeout, 
            CancellationToken cancellationToken)
        {
            return await ExecuteAcquireCommandAsync(connectionOrTransaction, resourceName, timeout, this.mode, cancellationToken).ConfigureAwait(false) ? Cookie : null;
        }

        ValueTask ISqlSynchronizationStrategy<object>.ReleaseAsync(ConnectionOrTransaction connectionOrTransaction, string resourceName, object lockCookie) =>
            ExecuteReleaseCommandAsync(connectionOrTransaction, resourceName);

        private static async Task<bool> ExecuteAcquireCommandAsync(ConnectionOrTransaction connectionOrTransaction, string lockName, TimeoutValue timeout, Mode mode, CancellationToken cancellationToken)
        {
            using var command = CreateAcquireCommand(connectionOrTransaction, lockName, timeout, mode, out var returnValue);
            await SqlHelpers.ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);
            return ParseExitCode((int)returnValue.Value);
        }

        private static async ValueTask ExecuteReleaseCommandAsync(ConnectionOrTransaction connectionOrTransaction, string lockName)
        {
            using var command = CreateReleaseCommand(connectionOrTransaction, lockName, out var returnValue);
            await SqlHelpers.ExecuteNonQueryAsync(command, CancellationToken.None).ConfigureAwait(false);
            ParseExitCode((int)returnValue.Value);
        }

        private static IDbCommand CreateAcquireCommand(
            ConnectionOrTransaction connectionOrTransaction, 
            string lockName, 
            TimeoutValue timeout, 
            Mode mode,
            out IDbDataParameter returnValue)
        {
            var command = connectionOrTransaction.Connection!.CreateCommand();
            command.Transaction = connectionOrTransaction.Transaction;
            command.CommandText = "dbo.sp_getapplock";
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = SqlHelpers.GetCommandTimeout(timeout);

            command.Parameters.Add(command.CreateParameter("Resource", lockName));
            command.Parameters.Add(command.CreateParameter("LockMode", GetModeString(mode)));
            command.Parameters.Add(command.CreateParameter("LockOwner", command.Transaction != null ? "Transaction" : "Session"));
            command.Parameters.Add(command.CreateParameter("LockTimeout", timeout.InMilliseconds));

            returnValue = command.CreateParameter();
            returnValue.Direction = ParameterDirection.ReturnValue;
            command.Parameters.Add(returnValue);

            return command;
        }

        private static IDbCommand CreateReleaseCommand(ConnectionOrTransaction connectionOrTransaction, string lockName, out IDbDataParameter returnValue)
        {
            var command = connectionOrTransaction.Connection!.CreateCommand();
            command.Transaction = connectionOrTransaction.Transaction;
            command.CommandText = "dbo.sp_releaseapplock";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(command.CreateParameter("Resource", lockName));
            command.Parameters.Add(command.CreateParameter("LockOwner", command.Transaction != null ? "Transaction" : "Session"));

            returnValue = command.CreateParameter();
            returnValue.Direction = ParameterDirection.ReturnValue;
            command.Parameters.Add(returnValue);

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

        private static string GetErrorMessage(int exitCode, string type) => $"The request for the distribute lock failed with exit code {exitCode} ({type})";

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
