using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    public class SqlDistributedSemaphore : IDistributedLock
    {
        private readonly SemaphoreHelper _helper;
        private readonly string _connectionString;

        public SqlDistributedSemaphore(string semaphoreName, int maxCount, string connectionString)
        {
            if (semaphoreName == null) { throw new ArgumentNullException(nameof(semaphoreName)); }
            if (maxCount <= 0) { throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "must be positive"); }
            // todo may be checked elsewhere later
            if (connectionString == null) { throw new ArgumentNullException(nameof(connectionString)); }

            this._helper = new SemaphoreHelper(semaphoreName, maxCount);
            this._connectionString = connectionString;
        }

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
            // TODO change this to not always go async
            return DistributedLockHelpers.TryAcquireWithAsyncCancellation(this, timeout, cancellationToken);
            //return cancellationToken.CanBeCanceled
            //    // use the async version since that supports cancellation
            //    ? DistributedLockHelpers.TryAcquireWithAsyncCancellation(this, timeout, cancellationToken)
            //    // synchronous mode
            //    : this.TryAcquireAsync(timeout).Task.Result;
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

        // todo comments
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
        public AwaitableDisposable<IDisposable> TryAcquireAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            return new AwaitableDisposable<IDisposable>(InternalTryAcquireAsync(timeout.ToInt32Timeout()));

            async Task<IDisposable> InternalTryAcquireAsync(int timeoutMillis)
            {
                // todo add better cleanup or switch to using internalocks structure
                var connection = new System.Data.SqlClient.SqlConnection(this._connectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                return await this._helper.TryAcquireAsync(connection, timeoutMillis, cancellationToken);
            }
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
        public AwaitableDisposable<IDisposable> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new AwaitableDisposable<IDisposable>(DistributedLockHelpers.AcquireAsync(this, timeout, cancellationToken));
        }

        public Task<int> GetCurrentCountAsync()
        {
            throw new NotImplementedException();
        }

        public int GetCurrentCount()
        {
            throw new NotImplementedException();
        }
        
        Task<IDisposable> IDistributedLock.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return this.TryAcquireAsync(timeout, cancellationToken).Task;
        }

        Task<IDisposable> IDistributedLock.AcquireAsync(TimeSpan? timeout, CancellationToken cancellationToken)
        {
            return this.AcquireAsync(timeout, cancellationToken).Task;
        }
    }
}
