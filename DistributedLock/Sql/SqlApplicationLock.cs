using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    internal static class SqlApplicationLock
    {
        public enum Mode
        {
            Shared,
            Update,
            Exclusive,
        }

        public static bool ExecuteAcquireCommand(ConnectionOrTransaction connectionOrTransaction, string lockName, int timeoutMillis, Mode mode)
        {
            IDbDataParameter returnValue;
            using (var command = CreateAcquireCommand(connectionOrTransaction, lockName, timeoutMillis, mode, out returnValue))
            {
                command.ExecuteNonQuery();
                return ParseExitCode((int)returnValue.Value);
            }
        }

        public static async Task<bool> ExecuteAcquireCommandAsync(ConnectionOrTransaction connectionOrTransaction, string lockName, int timeoutMillis, Mode mode, CancellationToken cancellationToken)
        {
            IDbDataParameter returnValue;
            using (var command = CreateAcquireCommand(connectionOrTransaction, lockName, timeoutMillis, mode, out returnValue))
            {
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                return ParseExitCode((int)returnValue.Value);
            }
        }

        public static void ExecuteReleaseCommand(ConnectionOrTransaction connectionOrTransaction, string lockName)
        {
            IDbDataParameter returnValue;
            using (var command = CreateReleaseCommand(connectionOrTransaction, lockName, out returnValue))
            {
                command.ExecuteNonQuery();
                ParseExitCode((int)returnValue.Value);
            }
        }

        public static async Task ExecuteReleaseCommandAsync(ConnectionOrTransaction connectionOrTransaction, string lockName)
        {
            IDbDataParameter returnValue;
            using (var command = CreateReleaseCommand(connectionOrTransaction, lockName, out returnValue))
            {
                await command.ExecuteNonQueryAsync(CancellationToken.None);
                ParseExitCode((int)returnValue.Value);
            }
        }

        public static IDbCommand CreateAcquireCommand(
            ConnectionOrTransaction connectionOrTransaction, 
            string lockName, 
            int timeoutMillis, 
            Mode mode,
            out IDbDataParameter returnValue)
        {
            var command = connectionOrTransaction.Connection.CreateCommand();
            command.Transaction = connectionOrTransaction.Transaction;
            command.CommandText = "dbo.sp_getapplock";
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = timeoutMillis >= 0
                  // command timeout is in seconds. We always wait at least the lock timeout plus a buffer 
                  ? (timeoutMillis / 1000) + 30
                  // otherwise timeout is infinite so we use the infinite timeout of 0
                  // (see https://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlcommand.commandtimeout%28v=vs.110%29.aspx)
                  : 0;

            command.Parameters.Add(command.CreateParameter("Resource", lockName));
            command.Parameters.Add(command.CreateParameter("LockMode", GetModeString(mode)));
            command.Parameters.Add(command.CreateParameter("LockOwner", command.Transaction != null ? "Transaction" : "Session"));
            command.Parameters.Add(command.CreateParameter("LockTimeout", timeoutMillis));

            returnValue = command.CreateParameter();
            returnValue.Direction = ParameterDirection.ReturnValue;
            command.Parameters.Add(returnValue);

            return command;
        }

        public static IDbCommand CreateReleaseCommand(ConnectionOrTransaction connectionOrTransaction, string lockName, out IDbDataParameter returnValue)
        {
            var command = connectionOrTransaction.Connection.CreateCommand();
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

                case -1: // timeout
                    return false;

                case -2: // canceled
                    throw new OperationCanceledException(GetErrorMessage(exitCode, "canceled"));
                case -3: // deadlock
                    throw new InvalidOperationException(GetErrorMessage(exitCode, "deadlock"));
                case -999: // parameter / unknown
                    throw new ArgumentException(GetErrorMessage(exitCode, "parameter validation or other error"));

                default:
                    if (exitCode <= 0)
                        throw new InvalidOperationException(GetErrorMessage(exitCode, "unknown"));
                    return true; // unknown success code
            }
        }

        private static string GetErrorMessage(int exitCode, string type)
        {
            return string.Format("The request for the distribute lock failed with exit code {0} ({1})", exitCode, type);
        }

        private static string GetModeString(Mode mode)
        {
            switch (mode)
            {
                case Mode.Shared: return "Shared";
                case Mode.Update: return "Update";
                case Mode.Exclusive: return "Exclusive";
                default: throw new ArgumentException(nameof(mode));
            }
        }
    }

    internal struct ConnectionOrTransaction
    {
        private object connectionOrTransaction;

        public IDbTransaction Transaction => this.connectionOrTransaction as IDbTransaction;
        public IDbConnection Connection => this.Transaction?.Connection ?? (this.connectionOrTransaction as IDbConnection);

        public ConnectionOrTransaction(IDbConnection connection)
        {
            if (connection == null) { throw new ArgumentNullException(nameof(connection)); }

            this.connectionOrTransaction = connection;
        }

        public ConnectionOrTransaction(IDbTransaction transaction)
        {
            if (transaction == null) { throw new ArgumentNullException(nameof(transaction)); }

            this.connectionOrTransaction = transaction;
        }

        public static implicit operator ConnectionOrTransaction(DbTransaction transaction) => new ConnectionOrTransaction(transaction);

        public static implicit operator ConnectionOrTransaction(DbConnection connection) => new ConnectionOrTransaction(connection);
    }
}
