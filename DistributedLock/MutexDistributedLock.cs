using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    // mutexes have thread afinity => that means that they can't support async locking

    internal sealed class MutexDistributedLock : IDistributedLock, IDisposable
    {
        private const string GlobalPrefix = @"Global\";

        private readonly Mutex mutex;
        private readonly bool suppressAbandonedMutexException;

        public MutexDistributedLock(string lockName, bool suppressAbandonedMutexException = true)
        {
            // note that just Global\ is not a valid name
            if (string.IsNullOrEmpty(lockName))
                throw new ArgumentNullException("lockName is required");
            if (lockName.Length > MaxLockNameLength)
                throw new FormatException("lockName: must be at most " + MaxLockNameLength + " characters");
            // from http://stackoverflow.com/questions/18392175/net-system-wide-eventwaithandle-name-allowed-characters
            if (lockName.IndexOf('\\') >= 0)
                throw new FormatException(@"lockName: must not contain '\'");

            this.mutex = new Mutex(initiallyOwned: false, name: GlobalPrefix + lockName);
            this.suppressAbandonedMutexException = true;
        }

        #region ---- Public API ----
        public static int MaxLockNameLength { get { return 260 - GlobalPrefix.Length; } }

        public void Dispose()
        {
            this.mutex.Dispose();
        }

        public IDisposable TryAcquire(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.CanBeCanceled)
            {
                // use the async version since that supports cancellation
                return DistributedLockHelpers.TryAcquireWithAsyncCancellation(this, timeout, cancellationToken);
            }

            var timeoutMillis = timeout.ToInt32Timeout();

            var acquired = this.suppressAbandonedMutexException
                ? this.WaitOneSafe(timeoutMillis)
                : this.mutex.WaitOne(timeoutMillis);

            return acquired ? new MutexScope(this.mutex) : null;
        }

        public IDisposable Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DistributedLockHelpers.Acquire(this, timeout, cancellationToken);
        }

        public Task<IDisposable> TryAcquireAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IDisposable> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DistributedLockHelpers.AcquireAsync(this, timeout, cancellationToken);
        }
        #endregion

        private bool WaitOneSafe(int timeoutMillis)
        { 
            var startMillis = Environment.TickCount;
            var remainingTimeoutMillis = timeoutMillis;

            do
            {
                try
                {
                    return this.mutex.WaitOne(timeoutMillis);
                }
                catch (AbandonedMutexException)
                {
                    // ignore
                }

                var elapsedMillis = Environment.TickCount - startMillis;
                remainingTimeoutMillis = timeoutMillis - elapsedMillis;
            }
            while (remainingTimeoutMillis > 0);

            return false; // timeout
        }

        private sealed class MutexScope : IDisposable
        {
            private Mutex mutex;

            public MutexScope(Mutex mutex)
            {
                this.mutex = mutex;
            }

            void IDisposable.Dispose()
            {
                var mutex = Interlocked.Exchange(ref this.mutex, null);
                if (mutex != null)
                {
                    mutex.ReleaseMutex();
                }
            }
        }
    }
}
