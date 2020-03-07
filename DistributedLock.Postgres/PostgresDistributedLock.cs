//using Medallion.Threading.Sql;
//using Npgsql;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Medallion.Threading.Postgres
//{
//    // todo more strategies
//    // todo more code sharing with SQL
//    // todo integrate into all appropriate abstract test cases (will want a new provider concept to abstract away pool clearing, credentials, DbProviderFactory, etc)

//    public sealed class PostgresDistributedLock : IDistributedLockOld
//    {
//        private readonly PostgresAdvisoryLockKey _key;
//        private readonly string _connectionString;

//        public PostgresDistributedLock(PostgresAdvisoryLockKey key, string connectionString)
//        {
//            if (connectionString == null) { throw new ArgumentNullException(nameof(connectionString)); }

//            this._key = key;
//            this._connectionString = connectionString;
//        }

//        public static PostgresAdvisoryLockKey GetSafeLockName(string name) => new PostgresAdvisoryLockKey(name, allowHashing: true);

//        /// <summary>
//        /// Acquires the lock synchronously, failing with <see cref="TimeoutException"/> if the attempt times out. Usage: 
//        /// <code>
//        ///     using (myLock.Acquire(...))
//        ///     {
//        ///         /* we have the lock! */
//        ///     }
//        ///     // dispose releases the lock
//        /// </code>
//        /// </summary>
//        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
//        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
//        /// <returns>An <see cref="IDisposable"/> which can be used to release the lock</returns>
//        public IDisposable Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
//            DistributedLockHelpersOld.Acquire(this, timeout, cancellationToken);

//        /// <summary>
//        /// Acquires the lock asynchronously, failing with <see cref="TimeoutException"/> if the attempt times out. Usage: 
//        /// <code>
//        ///     await using (await myLock.AcquireAsync(...))
//        ///     {
//        ///         /* we have the lock! */
//        ///     }
//        ///     // dispose releases the lock
//        /// </code>
//        /// </summary>
//        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
//        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
//        /// <returns>A <see cref="Task<IDisposable>"/> which can be used to release the lock</returns>
//        public Task<IDisposable> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
//            DistributedLockHelpersOld.AcquireAsync(this, timeout, cancellationToken);

//        /// <summary>
//        /// Attempts to acquire the lock synchronously. Usage: 
//        /// <code>
//        ///     using (var handle = myLock.TryAcquire(...))
//        ///     {
//        ///         if (handle != null) { /* we have the lock! */ }
//        ///     }
//        ///     // dispose releases the lock if we took it
//        /// </code>
//        /// </summary>
//        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to 0</param>
//        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
//        /// <returns>An <see cref="IDisposable"/> which can be used to release the lock or null on failure</returns>
//        public IDisposable? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default)
//        {
//            // todo fix
//            return this.TryAcquireAsync(timeout, cancellationToken).GetAwaiter().GetResult();
//        }

//        /// <summary>
//        /// Attempts to acquire the lock asynchronously. Usage: 
//        /// <code>
//        ///     await using (var handle = await myLock.TryAcquireAsync(...))
//        ///     {
//        ///         if (handle != null) { /* we have the lock! */ }
//        ///     }
//        ///     // dispose releases the lock if we took it
//        /// </code>
//        /// </summary>
//        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to 0</param>
//        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
//        /// <returns>A <see cref="Task<IDisposable?>"/> which can be used to release the lock or null on failure</returns>
//        public Task<IDisposable?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
//        {
//            timeout.ToInt32Timeout();
//            return DoTryAcquireAsync();

//            async Task<IDisposable?> DoTryAcquireAsync()
//            {
//                var connection = new NpgsqlConnection(this._connectionString);
//                var lockTaken = false;
//                try
//                {
//                    await connection.OpenAsync().ConfigureAwait(false);

//                    if (timeout >= TimeSpan.Zero)
//                    {
//                        using (var command = connection.CreateCommand())
//                        {
//                            this.PopulateAcquireCommand(command, isTry: true, isTransaction: false, isShared: false);
//                            var result = (bool)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
//                            if (result)
//                            {
//                                lockTaken = true;
//                                return new ConnectionLockScope(this, connection, isShared: false);
//                            }
//                            else if (timeout == TimeSpan.Zero)
//                            {
//                                return null;
//                            }
//                        }
//                    }

//                    using var timeoutSource = timeout > TimeSpan.Zero ? new CancellationTokenSource(timeout) : null;
//                    using var linkedSource = timeoutSource != null && cancellationToken.CanBeCanceled
//                        ? CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, cancellationToken)
//                        : null;
//                    var tokenToUse = linkedSource?.Token ?? timeoutSource?.Token ?? cancellationToken;
//                    using (var command = connection.CreateCommand())
//                    {
//                        this.PopulateAcquireCommand(command, isTry: false, isTransaction: false, isShared: false);
//                        try
//                        {
//                            await ExecuteNonQueryAsyncPropagateCancellation(command, tokenToUse).ConfigureAwait(false);
//                        }
//                        catch when (timeoutSource?.Token.IsCancellationRequested == true)
//                        {
//                            return null;
//                        }

//                        lockTaken = true;
//                        return new ConnectionLockScope(this, connection, isShared: false);
//                    }
//                }
//                finally
//                {
//                    if (!lockTaken)
//                    {
//                        await connection.DisposeAsync().ConfigureAwait(false);
//                    }
//                }
//            }
//        }

//        private void PopulateAcquireCommand(IDbCommand command, bool isTry, bool isTransaction, bool isShared)
//        {
//            // todo set command.timeout

//            this.PopulateKeyParameters(command, out var arguments);

//            command.CommandText =
//                $"SELECT pg{If(isTry, "_try")}_advisory{If(isTransaction, "_xact")}_lock{If(isShared, "_shared")}({arguments})";

//            static string If(bool condition, string conditional) => condition ? conditional : string.Empty;
//        }

//        private void PopulateReleaseCommand(IDbCommand command, bool isShared)
//        {
//            this.PopulateKeyParameters(command, out var arguments);

//            command.CommandText = $"SELECT pg_advisory_unlock{(isShared ? "_shared" : string.Empty)}({arguments})";
//        }

//        private void PopulateKeyParameters(IDbCommand command, out string arguments)
//        {
//            if (this._key.HasSingleKey)
//            {
//                command.Parameters.Add(command.CreateParameter("key", this._key.Key));
//                arguments = "@key";
//            }
//            else
//            {
//                var (key1, key2) = this._key.Keys;
//                command.Parameters.Add(command.CreateParameter("key1", key1));
//                command.Parameters.Add(command.CreateParameter("key2", key2));
//                arguments = "@key1, @key2";
//            }
//        }

//        // todo combine with SqlHelpers
//        private static async Task ExecuteNonQueryAsyncPropagateCancellation(NpgsqlCommand command, CancellationToken cancellationToken)
//        {
//            try
//            {
//                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
//            }
//            catch (PostgresException ex)
//                // cancellation error code from https://www.postgresql.org/docs/10/errcodes-appendix.html
//                when (cancellationToken.IsCancellationRequested && ex.SqlState == "57014")
//            {
//                throw new OperationCanceledException(
//                    "Command was canceled",
//                    ex,
//                    cancellationToken
//                );
//            }
//        }

//        private sealed class ConnectionLockScope : IDisposable
//        {
//            private PostgresDistributedLock? _lock;
//            private IDbConnection? _connection;
//            private readonly bool _isShared;

//            public ConnectionLockScope(PostgresDistributedLock @lock, IDbConnection connection, bool isShared)
//            {
//                this._lock = @lock;
//                this._connection = connection;
//                this._isShared = isShared;
//            }

//            public void Dispose()
//            {
//                var connection = Interlocked.Exchange(ref this._connection, null);
//                if (connection != null)
//                {
//                    var @lock = this._lock!;
//                    this._lock = null;

//                    try
//                    {
//                        using (var command = connection.CreateCommand())
//                        {
//                            @lock.PopulateReleaseCommand(command, this._isShared);
//                            if (!(bool)command.ExecuteScalar())
//                            {
//                                throw new InvalidOperationException($"Failed to release lock {@lock._key}");
//                            }
//                        }
//                    }
//                    finally
//                    {
//                        connection.Dispose();
//                    }
//                }
//            }
//        }
//    }
//}
