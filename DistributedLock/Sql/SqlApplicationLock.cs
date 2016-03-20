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
    internal sealed class SqlApplicationLock
    {
        public enum Mode
        {
            Mutex,
            Read,
            UpgradeableRead,
            Write,
        }

        private enum SqlLockMode
        {
            NoLock = -10000,
            Update = -10001,
            SharedIntentExclusive = -10002,
            IntentShared = -10003,
            IntentExclusive = -10004,
            UpdateIntentExclusive = -10005,
            Shared = -10006,
            Exclusive = -10007,
        }

        /// <summary>
        /// The maximum allowed length for lock names. See https://msdn.microsoft.com/en-us/library/ms189823.aspx
        /// </summary>
        public const int MaxLockNameLength = 255;

        private static readonly string CheckedGetAppLockQuery = $@"
            DECLARE @CurrentMode NVARCHAR(32) = APPLOCK_MODE('public', @LockName, @LockOwner);
            IF (
                    (@LockMode IN ('{SqlLockMode.Shared}', '{SqlLockMode.Update}') AND @CurrentMode = '{SqlLockMode.NoLock}')
                    OR (@LockMode = '{SqlLockMode.Exclusive}' AND @CurrentMode IN ('{SqlLockMode.NoLock}', '{SqlLockMode.Update}'))
               )
                @Result = CASE @CurrentMode {string.Join(
                    " ", 
                    Enum.GetValues(typeof(SqlLockMode))
                        .Cast<SqlLockMode>()
                        .Select(m => $"WHEN '{m.ToString()}' THEN {(int)m}")
                )} END;
            ELSE
                EXEC @Result = sp_getapplock
                    @Resource = @Resource
                    , @LockMode = @LockMode
                    , @LockOwner = @LockOwner
                    , @LockTimeout = @LockTimeout;
        ";

        private readonly string lockName;

        // depending on the mode we're in, only one of connection string, 
        // connection, and transaction is ever populated

        private readonly string connectionString;
        private readonly DbConnection connection;
        private readonly DbTransaction transaction;

        /// <summary>
        /// Creates a lock with name <paramref name="lockName"/>, using the given
        /// <paramref name="connectionString"/> to connect to the database
        /// </summary>
        public SqlApplicationLock(string lockName, string connectionString)
            : this(lockName)
        {
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Creates a lock with name <paramref name="lockName"/> which, when acquired,
        /// will be scoped to the given <see cref="connection"/>. The <paramref name="connection"/> is
        /// assumed to be externally managed: the <see cref="SqlApplicationLock"/> will not attempt to open,
        /// close, or dispose it
        /// </summary>
        public SqlApplicationLock(string lockName, DbConnection connection)
            : this(lockName)
        {
            this.connection = connection;
        }

        /// <summary>
        /// Creates a lock with name <paramref name="lockName"/> which, when acquired,
        /// will be scoped to the given <paramref name="transaction"/>. The <paramref name="transaction"/> and its
        /// <see cref="DbTransaction.Connection"/> are assumed to be externally managed: the <see cref="SqlDistributedLock"/> will 
        /// not attempt to open, close, commit, roll back, or dispose them
        /// </summary>
        public SqlApplicationLock(string lockName, DbTransaction transaction)
            : this(lockName)
        {
            this.transaction = transaction;
        }

        private SqlApplicationLock(string lockName)
        {
            this.lockName = lockName;
        }

        public IDisposable TryAcquire(Mode mode, TimeSpan timeout)
        {
            // synchronous mode
            var timeoutMillis = timeout.ToInt32Timeout();

            DbConnection connection = null;
            DbTransaction transaction = null;
            var cleanup = true;
            try
            {
                connection = this.GetConnection();
                if (this.connectionString != null)
                {
                    connection.Open();
                }
                else if (connection == null)
                    throw new InvalidOperationException("The transaction had been disposed");
                else if (connection.State != ConnectionState.Open)
                    throw new InvalidOperationException("The connection is not open");

                transaction = this.GetTransaction(connection);
                SqlParameter returnValue;
                using (var command = CreateAcquireCommand(connection, transaction, this.lockName, timeoutMillis, mode, out returnValue))
                {
                    command.ExecuteNonQuery();
                    var exitCode = (int)returnValue.Value;
                    if (ParseExitCode(exitCode))
                    {
                        cleanup = false;
                        return new LockScope(this, transaction);
                    }
                    return null;
                }
            }
            catch
            {
                // in case we fail to create lock scope or something
                cleanup = true;
                throw;
            }
            finally
            {
                if (cleanup)
                {
                    this.Cleanup(transaction, connection);
                }
            }
        }

        public Task<IDisposable> TryAcquireAsync(Mode mode, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var timeoutMillis = timeout.ToInt32Timeout();

            return this.InternalTryAcquireAsync(mode, timeoutMillis, cancellationToken);
        }
                
        private async Task<IDisposable> InternalTryAcquireAsync(Mode mode, int timeoutMillis, CancellationToken cancellationToken)
        {
            // it's important that this happens in the async method so that we cancel instead of throwing
            cancellationToken.ThrowIfCancellationRequested();

            DbConnection connection = null;
            DbTransaction transaction = null;
            var cleanup = true;
            try
            {
                connection = this.GetConnection();

                if (this.connectionString != null)
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                }
                else if (connection == null)
                    throw new InvalidOperationException("The transaction had been disposed");
                else if (connection.State != ConnectionState.Open)
                    throw new InvalidOperationException("The connection is not open");

                transaction = this.GetTransaction(connection);
                SqlParameter returnValue;
                using (var command = CreateAcquireCommand(connection, transaction, this.lockName, timeoutMillis, mode, out returnValue))
                {
                    await command.ExecuteNonQueryAndPropagateCancellationAsync(cancellationToken).ConfigureAwait(false);
                    var exitCode = (int)returnValue.Value;
                    if (ParseExitCode(exitCode))
                    {
                        cleanup = false;
                        return new LockScope(this, transaction);
                    }
                    return null;
                }
            }
            catch
            {
                // just in case we failed to create scope or something
                cleanup = true;
                throw;
            }
            finally
            {
                if (cleanup)
                {
                    this.Cleanup(transaction, connection);
                }
            }
        }

        private void Cleanup(DbTransaction transaction, DbConnection connection)
        {
            // dispose connection and transaction unless they are externally owned
            if (this.connectionString != null)
            {
                if (transaction != null)
                {
                    transaction.Dispose();
                }
                if (connection != null)
                {
                    connection.Dispose();
                }
            }
        }

        private void ReleaseLock(DbTransaction transaction)
        {
            if (this.connectionString != null)
            {
                // if we own the connection, just dispose the connection & transaction
                var connection = transaction.Connection;
                transaction.Dispose();
                connection.Dispose();
            }
            else
            {
                // otherwise issue the release command

                var connection = this.GetConnection();
                // if the connection/transaction was closed, the lock was already released so we're good!
                if (connection != null && connection.State == ConnectionState.Open)
                {
                    SqlParameter returnValue;
                    using (var command = CreateReleaseCommand(connection, this.transaction, this.lockName, out returnValue))
                    {
                        command.ExecuteNonQuery();
                        var exitCode = (int)returnValue.Value;
                        ParseExitCode(exitCode);
                    }
                }
            }
        }

        private static bool ParseExitCode(int exitCode)
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

                case (int)SqlLockMode.Exclusive:
                case (int)SqlLockMode.IntentExclusive:
                case (int)SqlLockMode.IntentShared:
                case (int)SqlLockMode.NoLock:
                case (int)SqlLockMode.Shared:
                case (int)SqlLockMode.SharedIntentExclusive:
                case (int)SqlLockMode.Update:
                case (int)SqlLockMode.UpdateIntentExclusive:
                    throw new InvalidOperationException($"The current lock state '{(SqlLockMode)exitCode}' is not valid for the current operation");

                default:
                    if (exitCode <= 0)
                        throw new InvalidOperationException(GetErrorMessage(exitCode, "unknown"));
                    return true; // unknown success code
            }
        }

        private static string GetErrorMessage(int exitCode, string type)
        {
            return $"The request for the distribute lock failed with exit code {exitCode} ({type})";
        }

        private DbConnection GetConnection()
        {
            return this.connectionString != null ? new SqlConnection(this.connectionString)
                : this.connection != null ? this.connection
                : this.transaction.Connection;
        }

        private DbTransaction GetTransaction(DbConnection connection)
        {
            // when creating a transaction, the isolation level doesn't matter, since we're using sp_getapplock
            return this.connectionString != null ? connection.BeginTransaction(IsolationLevel.ReadUncommitted)
                : this.connection != null ? null
                : this.transaction;
        }

        private static DbCommand CreateAcquireCommand(
            DbConnection connection, 
            DbTransaction transaction, 
            string lockName, 
            int timeoutMillis, 
            Mode mode,
            out SqlParameter returnValue)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            if (mode == Mode.Mutex)
            {
                command.CommandText = "dbo.sp_getapplock";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(CreateParameter(command, "LockMode", SqlLockMode.Exclusive.ToString()));
                command.Parameters.Add(returnValue = new SqlParameter { Direction = ParameterDirection.ReturnValue });
            }
            else
            {
                command.CommandText = CheckedGetAppLockQuery;
                command.CommandType = CommandType.Text;
                SqlLockMode lockMode;
                switch (mode)
                {
                    case Mode.Read:
                        lockMode = SqlLockMode.Shared;
                        break;
                    case Mode.UpgradeableRead:
                        lockMode = SqlLockMode.Update;
                        break;
                    case Mode.Write:
                        lockMode = SqlLockMode.Exclusive;
                        break;
                    default:
                        throw new ArgumentException($"Unexpected mode {mode}", nameof(mode));
                }
                command.Parameters.Add(CreateParameter(command, "LockMode", lockMode.ToString()));
                command.Parameters.Add(returnValue = new SqlParameter { ParameterName = "Result", Direction = ParameterDirection.Output });
            }

            command.CommandTimeout = timeoutMillis >= 0
                  // command timeout is in seconds. We always wait at least the lock timeout plus a buffer 
                  ? (timeoutMillis / 1000) + 30
                  // otherwise timeout is infinite so we use the infinite timeout of 0
                  // (see https://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlcommand.commandtimeout%28v=vs.110%29.aspx)
                  : 0;

            command.Parameters.Add(CreateParameter(command, "Resource", lockName));
            command.Parameters.Add(CreateParameter(command, "LockOwner", transaction != null ? "Transaction" : "Session"));
            command.Parameters.Add(CreateParameter(command, "LockTimeout", timeoutMillis));
            
            return command;
        }

        private static DbCommand CreateReleaseCommand(DbConnection connection, DbTransaction transaction, string lockName, out SqlParameter returnValue)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "dbo.sp_releaseapplock";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(CreateParameter(command, "Resource", lockName));
            command.Parameters.Add(CreateParameter(command, "LockOwner", transaction != null ? "Transaction" : "Session"));
            command.Parameters.Add(returnValue = new SqlParameter { Direction = ParameterDirection.ReturnValue });

            return command;
        }

        private static DbParameter CreateParameter(DbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            return parameter;
        }

        private sealed class LockScope : IDisposable
        {
            private SqlApplicationLock @lock;
            private DbTransaction transaction;

            public LockScope(SqlApplicationLock @lock, DbTransaction transaction)
            {
                this.@lock = @lock;
                this.transaction = transaction;
            }

            void IDisposable.Dispose()
            {
                var @lock = Interlocked.Exchange(ref this.@lock, null);
                if (@lock != null)
                {
                    var transaction = this.transaction;
                    this.transaction = null;
                    @lock.ReleaseLock(transaction);
                }
            }
        }
    }
}
