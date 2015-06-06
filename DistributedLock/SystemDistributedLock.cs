using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    // TODO abandonment: wait for shorter times and reconstitute the event at that point

    public sealed class SystemDistributedLock : IDistributedLock
    {
        private const string GlobalPrefix = @"Global\";
        private static readonly TimeSpan DefaultAbandonmentCheckFrequency = TimeSpan.FromSeconds(2);

        private readonly string lockName;
        private readonly TimeSpan abandonmentCheckFrequency;

        public SystemDistributedLock(string lockName, TimeSpan? abandonmentCheckFrequency = default(TimeSpan?))
        {
            // note that just Global\ is not a valid name
            if (string.IsNullOrEmpty(lockName))
                throw new ArgumentNullException("lockName is required");
            if (lockName.Length > MaxLockNameLength)
                throw new FormatException("lockName: must be at most " + MaxLockNameLength + " characters");
            // from http://stackoverflow.com/questions/18392175/net-system-wide-eventwaithandle-name-allowed-characters
            if (lockName.IndexOf('\\') >= 0)
                throw new FormatException(@"lockName: must not contain '\'");

            if (abandonmentCheckFrequency.HasValue)
            {
                // must be a valid timeout
                var abandonmentCheckFrequencyMillis = abandonmentCheckFrequency.Value.ToInt32Timeout("abandonmentCheckFrequency");
                if (abandonmentCheckFrequencyMillis == 0)
                    throw new ArgumentOutOfRangeException("abandonmentCheckFrequency: must be non-zero");
                this.abandonmentCheckFrequency = abandonmentCheckFrequency.Value;
            }
            else
            {
                this.abandonmentCheckFrequency = DefaultAbandonmentCheckFrequency;
            }

            this.lockName = GlobalPrefix + lockName;
        }

        #region ---- Public API ----
        public static int MaxLockNameLength { get { return 260 - GlobalPrefix.Length; } }

        public static string GetSafeLockName(string baseLockName)
        {
            return DistributedLockHelpers.ToSafeLockName(baseLockName, MaxLockNameLength, s => s.Length == 0 ? "EMPTY" : s.Replace('\\', '_'));
        }

        public IDisposable TryAcquire(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            var timeoutMillis = timeout.ToInt32Timeout();
            var abandonmentCheckFrequencyMillis = this.abandonmentCheckFrequency.ToInt32Timeout();
            
            var @event = this.CreateEvent();
            var cleanup = true;
            try
            {
                if (abandonmentCheckFrequencyMillis <= 0)
                {
                    // no abandonment check: just acquire once
                    if (TryAcquireOnce(@event, timeoutMillis, cancellationToken))
                    {
                        cleanup = false;
                        return new EventScope(@event);
                    }
                    return null;
                }

                if (timeoutMillis < 0)
                {
                    // infinite timeout: just loop forever with the abandonment check
                    while (true)
                    {
                        if (TryAcquireOnce(@event, abandonmentCheckFrequencyMillis, cancellationToken))
                        {
                            cleanup = false;
                            return new EventScope(@event);
                        }

                        // refresh the event in case it was abandoned by the original owner
                        @event.Dispose();
                        @event = this.CreateEvent();
                    }
                }

                // fixed timeout: loop in abandonment check chunks
                var elapsedMillis = 0;
                do
                {
                    var nextWaitMillis = Math.Min(abandonmentCheckFrequencyMillis, timeoutMillis - elapsedMillis);
                    if (TryAcquireOnce(@event, nextWaitMillis, cancellationToken))
                    {
                        cleanup = false;
                        return new EventScope(@event);
                    }

                    elapsedMillis += nextWaitMillis;

                    // refresh the event in case it was abandoned by the original owner
                    @event.Dispose();
                    @event = this.CreateEvent();
                }
                while (elapsedMillis < timeoutMillis);

                return null;
            }
            catch
            {
                // just in case we fail to create a scope or something
                cleanup = true;
                throw;
            }
            finally
            {
                if (cleanup)
                {
                    @event.Dispose();
                }
            }
        }

        public IDisposable Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DistributedLockHelpers.Acquire(this, timeout, cancellationToken);
        }

        public Task<IDisposable> TryAcquireAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            timeout.ToInt32Timeout(); // validate

            return this.InternalTryAcquireAsync(timeout, cancellationToken);
        }

        public Task<IDisposable> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DistributedLockHelpers.AcquireAsync(this, timeout, cancellationToken);
        }
        #endregion

        private async Task<IDisposable> InternalTryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var @event = this.CreateEvent();
            var cleanup = true;
            try
            {
                if (await @event.WaitOneAsync(timeout, cancellationToken).ConfigureAwait(false))
                {
                    cleanup = false;
                    return new EventScope(@event);
                }

                return null;
            }
            catch
            {
                // just in case we fail to create a scope or something
                cleanup = true;
                throw;
            }
            finally
            {
                if (cleanup)
                {
                    @event.Dispose();
                }
            }
        }

        private static bool TryAcquireOnce(EventWaitHandle @event, int timeoutMillis, CancellationToken cancellationToken)
        {
            // cancellation case
            if (cancellationToken.CanBeCanceled)
            {
                // ensures that if we are already canceled upon entering this method
                // we will cancel, not wait
                cancellationToken.ThrowIfCancellationRequested();

                // cancellable wait based on
                // http://www.thomaslevesque.com/2015/06/04/async-and-cancellation-support-for-wait-handles/
                var index = WaitHandle.WaitAny(new[] { @event, cancellationToken.WaitHandle }, timeoutMillis);
                switch (index)
                {
                    case WaitHandle.WaitTimeout: // timeout
                        @event.Dispose();
                        return false;
                    case 0: // event
                        return true;
                    default: // canceled
                        cancellationToken.ThrowIfCancellationRequested();
                        throw new InvalidOperationException("Should never get here");
                }
            }

            // normal case
            return @event.WaitOne(timeoutMillis);
        }

        private EventWaitHandle CreateEvent()
        {
            // based on http://stackoverflow.com/questions/2590334/creating-a-cross-process-eventwaithandle
            var security = new EventWaitHandleSecurity();
            // allow anyone to wait on and signal this lock
            security.AddAccessRule(
                new EventWaitHandleAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, domainSid: null), 
                    EventWaitHandleRights.FullControl, // doesn't seem to work without this :-/
                    AccessControlType.Allow
                )
            );

            bool ignored;
            var @event = new EventWaitHandle(
                // if we create, start as unlocked
                initialState: true,
                // allow only one thread to hold the lock
                mode: EventResetMode.AutoReset,
                name: this.lockName,
                createdNew: out ignored,
                eventSecurity: security
            );

            return @event;
        }

        private sealed class EventScope : IDisposable
        {
            private EventWaitHandle @event;

            public EventScope(EventWaitHandle @event) 
            {
                this.@event = @event;
            }

            void IDisposable.Dispose()
            {
                var @event = Interlocked.Exchange(ref this.@event, null);
                if (@event != null)
                {
                    @event.Set(); // signal
                    @event.Dispose();
                }
            }
        }
    }
}
