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
    /// <summary>
    /// Implements a distributed lock using a SQL server application lock
    /// (see https://msdn.microsoft.com/en-us/library/ms189823.aspx)
    /// </summary>
    public sealed class SqlDistributedLock : IDistributedLock
    {
        private readonly string lockName, connectionString;

        private readonly DbConnection connection;
        private readonly DbTransaction transaction;

        /// <summary>
        /// Creates a lock with name <paramref name="lockName"/>, using the given
        /// <paramref name="connectionString"/> to connect to the database
        /// </summary>
        public SqlDistributedLock(string lockName, string connectionString)
            : this(lockName)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException("connectionString");

            this.connectionString = connectionString;
        }

        /// <summary>
        /// Creates a lock with name <paramref name="lockName"/> which, when acquired,
        /// will be scoped to the given <see cref="connection"/>
        /// </summary>
        public SqlDistributedLock(string lockName, DbConnection connection)
            : this(lockName)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");

            this.connection = connection;
        }

        /// <summary>
        /// Creates a lock with name <paramref name="lockName"/> which, when acquired,
        /// will be scoped to the given <paramref name="transaction"/>
        /// </summary>
        public SqlDistributedLock(string lockName, DbTransaction transaction)
            : this(lockName)
        {
            if (transaction == null)
                throw new ArgumentNullException("transaction");

            this.transaction = transaction;
        }

        private SqlDistributedLock(string lockName)
        {
            if (lockName == null)
                throw new ArgumentNullException("lockName");
            if (lockName.Length > MaxLockNameLength)
                throw new FormatException("lockName: must be at most " + MaxLockNameLength + " characters");

            this.lockName = lockName;
        }

        #region ---- Public API ----
        /// <summary>
        /// Attempts to acquire the lock synchronously. Usage:
        /// <code>
        ///     using (var handle = myLock.TryAcquire(...))
        ///     {
        ///         if (handle != null) { /* we have the lock! */ }
        ///     }
        ///     // dispose releases the lock if we took it
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on acquiring the lock. Defaults to 0</param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>An <see cref="IDisposable"/> "handle" which can be used to release the lock, or null if the lock was not taken</returns>
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
                using (var command = CreateAcquireCommand(connection, transaction, this.lockName, timeoutMillis, out returnValue))
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

        /// <summary>
        /// Acquires the lock synchronously, failing with <see cref="TimeoutException"/> if the wait times out
        /// <code>
        ///     using (myLock.Acquire(...))
        ///     {
        ///         // we have the lock
        ///     }
        ///     // dispose releases the lock
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on acquiring the lock. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>An <see cref="IDisposable"/> "handle" which can be used to release the lock</returns>
        public IDisposable Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DistributedLockHelpers.Acquire(this, timeout, cancellationToken);
        }

        /// <summary>
        /// Attempts to acquire the lock asynchronously. Usage:
        /// <code>
        ///     using (var handle = await myLock.TryAcquireAsync(...))
        ///     {
        ///         if (handle != null) { /* we have the lock! */ }
        ///     }
        ///     // dispose releases the lock if we took it
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on acquiring the lock. Defaults to 0</param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>An <see cref="IDisposable"/> "handle" which can be used to release the lock, or null if the lock was not taken</returns>
        public Task<IDisposable> TryAcquireAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            var timeoutMillis = timeout.ToInt32Timeout();

            return this.InternalTryAcquireAsync(timeoutMillis, cancellationToken);
        }

        /// <summary>
        /// Acquires the lock asynchronously, failing with <see cref="TimeoutException"/> if the wait times out
        /// <code>
        ///     using (await myLock.AcquireAsync(...))
        ///     {
        ///         // we have the lock
        ///     }
        ///     // dispose releases the lock
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on acquiring the lock. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>An <see cref="IDisposable"/> "handle" which can be used to release the lock</returns>
        public Task<IDisposable> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DistributedLockHelpers.AcquireAsync(this, timeout, cancellationToken);
        }
        
        /// <summary>
        /// The maximum allowed length for lock names. See https://msdn.microsoft.com/en-us/library/ms189823.aspx
        /// </summary>
        public static int MaxLockNameLength { get { return 255; } }

        /// <summary>
        /// Given <paramref name="baseLockName"/>, constructs a lock name which is safe for use with <see cref="SqlDistributedLock"/>
        /// </summary>
        public static string GetSafeLockName(string baseLockName)
        {
            return DistributedLockHelpers.ToSafeLockName(baseLockName, MaxLockNameLength, s => s);
        }
        #endregion

        private async Task<IDisposable> InternalTryAcquireAsync(int timeoutMillis, CancellationToken cancellationToken)
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
                using (var command = CreateAcquireCommand(connection, transaction, this.lockName, timeoutMillis, out returnValue))
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

        private static DbCommand CreateAcquireCommand(DbConnection connection, DbTransaction transaction, string lockName, int timeoutMillis, out SqlParameter returnValue)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
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
            command.Parameters.Add(CreateParameter(command, "LockOwner", transaction != null ? "Transaction" : "Session"));
            command.Parameters.Add(CreateParameter(command, "LockTimeout", timeoutMillis));
            command.Parameters.Add(returnValue = new SqlParameter { Direction = ParameterDirection.ReturnValue });

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
            private SqlDistributedLock @lock;
            private DbTransaction transaction;

            public LockScope(SqlDistributedLock @lock, DbTransaction transaction)
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
