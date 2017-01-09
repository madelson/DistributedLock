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
        public static bool ExecuteAcquireCommand(ConnectionOrTransaction connectionOrTransaction, string lockName, int timeoutMillis)
        {
            DbParameter returnValue;
            using (var command = CreateAcquireCommand(connectionOrTransaction, lockName, timeoutMillis, out returnValue))
            {
                command.ExecuteNonQuery();
                return ParseExitCode((int)returnValue.Value);
            }
        }

        public static async Task<bool> ExecuteAcquireCommandAsync(ConnectionOrTransaction connectionOrTransaction, string lockName, int timeoutMillis, CancellationToken cancellationToken)
        {
            DbParameter returnValue;
            using (var command = CreateAcquireCommand(connectionOrTransaction, lockName, timeoutMillis, out returnValue))
            {
                await command.ExecuteNonQueryAndPropagateCancellationAsync(cancellationToken).ConfigureAwait(false);
                return ParseExitCode((int)returnValue.Value);
            }
        }

        public static void ExecuteReleaseCommand(ConnectionOrTransaction connectionOrTransaction, string lockName)
        {
            DbParameter returnValue;
            using (var command = CreateReleaseCommand(connectionOrTransaction, lockName, out returnValue))
            {
                command.ExecuteNonQuery();
                ParseExitCode((int)returnValue.Value);
            }
        }

        public static DbCommand CreateAcquireCommand(ConnectionOrTransaction connectionOrTransaction, string lockName, int timeoutMillis, out DbParameter returnValue)
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

            command.Parameters.Add(CreateParameter(command, "Resource", lockName));
            command.Parameters.Add(CreateParameter(command, "LockMode", "Exclusive"));
            command.Parameters.Add(CreateParameter(command, "LockOwner", command.Transaction != null ? "Transaction" : "Session"));
            command.Parameters.Add(CreateParameter(command, "LockTimeout", timeoutMillis));

            returnValue = command.CreateParameter();
            returnValue.Direction = ParameterDirection.ReturnValue;
            command.Parameters.Add(returnValue);

            return command;
        }

        public static DbCommand CreateReleaseCommand(ConnectionOrTransaction connectionOrTransaction, string lockName, out DbParameter returnValue)
        {
            var command = connectionOrTransaction.Connection.CreateCommand();
            command.Transaction = connectionOrTransaction.Transaction;
            command.CommandText = "dbo.sp_releaseapplock";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(CreateParameter(command, "Resource", lockName));
            command.Parameters.Add(CreateParameter(command, "LockOwner", command.Transaction != null ? "Transaction" : "Session"));

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

        private static DbParameter CreateParameter(DbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            return parameter;
        }
    }

    internal struct ConnectionOrTransaction
    {
        private object connectionOrTransaction;

        public DbTransaction Transaction => this.connectionOrTransaction as DbTransaction;
        public DbConnection Connection => this.Transaction?.Connection ?? (this.connectionOrTransaction as DbConnection);

        public static implicit operator ConnectionOrTransaction(DbTransaction transaction)
        {
            if (transaction == null) { throw new ArgumentNullException(nameof(transaction)); }

            return new ConnectionOrTransaction { connectionOrTransaction = transaction };
        }

        public static implicit operator ConnectionOrTransaction(DbConnection connection)
        {
            if (connection == null) { throw new ArgumentNullException(nameof(connection)); }

            return new ConnectionOrTransaction { connectionOrTransaction = connection };
        }
    }
}
