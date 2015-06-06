using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    public sealed class SqlDistributedLock : IDistributedLock
    {
        private readonly string lockName, connectionString;

        public SqlDistributedLock(string lockName, string connectionString)
        {
            ValidateLockName(lockName);
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException("connectionString");

            this.lockName = lockName;
            this.connectionString = connectionString;
        }

        #region ---- Public API ----
        public IDisposable TryAcquire(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.CanBeCanceled)
            {
                // use the async version since that supports cancellation
                return DistributedLockHelpers.TryAcquireWithAsyncCancellation(this, timeout, cancellationToken);
            }

            // synchronous mode
            var timeoutMillis = timeout.ToInt32Timeout();

            DbConnection connection = null;
            DbTransaction transaction = null;
            var cleanup = true;
            try
            {
                connection = this.CreateConnection();
                connection.Open();
                
                transaction = CreateTransaction(connection);
                SqlParameter returnValue;
                using (var command = CreateCommand(transaction, this.lockName, timeoutMillis, out returnValue))
                {
                    command.ExecuteNonQuery();
                    var exitCode = (int)returnValue.Value;
                    if (ParseExitCode(exitCode))
                    {
                        cleanup = false;
                        return new LockScope(transaction);
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
                    Cleanup(transaction, connection);
                }
            }
        }

        public IDisposable Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DistributedLockHelpers.Acquire(this, timeout, cancellationToken);
        }

        public Task<IDisposable> TryAcquireAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            var timeoutMillis = timeout.ToInt32Timeout();

            cancellationToken.ThrowIfCancellationRequested();

            return this.InternalTryAcquireAsync(timeoutMillis, cancellationToken);
        }

        public Task<IDisposable> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DistributedLockHelpers.AcquireAsync(this, timeout, cancellationToken);
        }
        
        /// <summary>
        /// The maximum allowed length for lock names. See https://msdn.microsoft.com/en-us/library/ms189823.aspx
        /// </summary>
        public static int MaxLockNameLength { get { return 255; } }
        #endregion

        private async Task<IDisposable> InternalTryAcquireAsync(int timeoutMillis, CancellationToken cancellationToken)
        {
            DbConnection connection = null;
            DbTransaction transaction = null;
            var cleanup = true;
            try
            {
                connection = this.CreateConnection();

                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                transaction = CreateTransaction(connection);
                SqlParameter returnValue;
                using (var command = CreateCommand(transaction, this.lockName, timeoutMillis, out returnValue))
                {
                    await command.ExecuteNonQueryAndPropagateCancellationAsync(cancellationToken).ConfigureAwait(false);
                    var exitCode = (int)returnValue.Value;
                    if (ParseExitCode(exitCode))
                    {
                        cleanup = false;
                        return new LockScope(transaction);
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
                    Cleanup(transaction, connection);
                }
            }
        }

        private static void Cleanup(DbTransaction transaction, DbConnection connection)
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

        internal static void ValidateLockName(string lockName)
        {
            if (lockName == null)
                throw new ArgumentNullException("lockName");
            if (lockName.Length > MaxLockNameLength)
                throw new FormatException("lockName: must be at most " + MaxLockNameLength + " characters");
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

                default:
                    if (exitCode <= 0)
                        throw new InvalidOperationException(GetErrorMessage(exitCode, "unknown"));
                    return true; // unknown success code
            }
        }

        private static string GetErrorMessage(int exitCode, string type)
        {
            return string.Format("The request to acquire the distribute lock failed with exit code {0} ({1})", exitCode, type);
        }

        private DbConnection CreateConnection()
        {
            return new SqlConnection(this.connectionString);
        }

        private static DbTransaction CreateTransaction(DbConnection connection)
        {
            // the isolation level of the transaction doesn't matter, since we're using sp_getapplock
            return connection.BeginTransaction(IsolationLevel.ReadUncommitted);
        }

        private static DbCommand CreateCommand(DbTransaction transaction, string lockName, int timeoutMillis, out SqlParameter returnValue)
        {
            var command = transaction.Connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "dbo.sp_getapplock";
            command.CommandType = CommandType.StoredProcedure;
            // command timeout is in seconds. We always wait at least the lock timeout plus a buffer
            command.CommandTimeout = (timeoutMillis / 1000) + 30;

            command.Parameters.Add(CreateParameter(command, "Resource", lockName));
            command.Parameters.Add(CreateParameter(command, "LockMode", "Exclusive"));
            command.Parameters.Add(CreateParameter(command, "LockTimeout", timeoutMillis));
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
            private DbTransaction transaction;

            public LockScope(DbTransaction transaction)
            {
                this.transaction = transaction;
            }

            void IDisposable.Dispose()
            {
                var transaction = Interlocked.Exchange(ref this.transaction, null);
                if (transaction != null)
                {
                    var connection = transaction.Connection;
                    transaction.Dispose();
                    connection.Dispose();
                }
            }
        }
    }
}
