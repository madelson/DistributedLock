using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.WaitHandles
{
    public sealed partial class EventWaitHandleDistributedLock : IInternalDistributedLock<EventWaitHandleDistributedLockHandle>
    {
        private const string GlobalPrefix = @"Global\";
        private static readonly TimeoutValue DefaultAbandonmentCheckCadence = TimeSpan.FromSeconds(2);

        private readonly TimeoutValue _abandonmentCheckCadence;

        public EventWaitHandleDistributedLock(string name, TimeSpan? abandonmentCheckCadence = null, bool exactName = false)
        {
            if (exactName)
            {
                if (name == null) { throw new ArgumentNullException(nameof(name)); }
                if (name.Length == 0) { throw new FormatException(nameof(name) + ": must not be empty"); }
                if (name.Length > MaxNameLength) { throw new FormatException($"{nameof(name)}: must be at most {MaxNameLength} characters"); }
                // todo confirm ignorecase
                if (!name.StartsWith(GlobalPrefix, StringComparison.OrdinalIgnoreCase)) { throw new FormatException($"{nameof(name)}: must start with '{GlobalPrefix}'"); }
                if (name.IndexOf('\\') >= 0) { throw new FormatException(nameof(name) + @": must not contain '\'"); }

                this.Name = name;
            }
            else
            {
                this.Name = GetSafeLockName(name);
            }

            TimeoutValue abandonmentCheckCadenceTimeout;
            // try-catch to get a better error message
            try { abandonmentCheckCadenceTimeout = new TimeoutValue(abandonmentCheckCadence); }
            catch { throw new ArgumentOutOfRangeException(nameof(abandonmentCheckCadence)); }
            if (abandonmentCheckCadenceTimeout.IsZero) { throw new ArgumentOutOfRangeException(nameof(abandonmentCheckCadence), "must not be zero"); }
            this._abandonmentCheckCadence = abandonmentCheckCadenceTimeout;
        }

        /// <summary>
        /// The maximum allowed length for lock names
        /// </summary>
        // 260 based on LINQPad experimentation
        public static int MaxNameLength => 260 - GlobalPrefix.Length;

        public string Name { get; }

        bool IDistributedLock.IsReentrant => false;

        public static string GetSafeLockName(string name) =>
            DistributedLockHelpers.ToSafeLockName(name, MaxNameLength, s => s.Length == 0 ? "EMPTY" : s.Replace('\\', '_'));

        async ValueTask<EventWaitHandleDistributedLockHandle?> IInternalDistributedLock<EventWaitHandleDistributedLockHandle>.InternalTryAcquireAsync(
            TimeoutValue timeout, 
            CancellationToken cancellationToken)
        {
            var @event = this.CreateEvent();
            var cleanup = true;
            try
            {
                if (this._abandonmentCheckCadence.IsInfinite)
                {
                    // no abandonment check: just acquire once
                    if (await @event.WaitOneAsync(timeout, cancellationToken).ConfigureAwait(false))
                    {
                        cleanup = false;
                        return new EventWaitHandleDistributedLockHandle(@event);
                    }
                    return null;
                }

                if (timeout.IsInfinite)
                {
                    // infinite timeout: just loop forever with the abandonment check
                    while (true)
                    {
                        if (await @event.WaitOneAsync(this._abandonmentCheckCadence, cancellationToken).ConfigureAwait(false))
                        {
                            cleanup = false;
                            return new EventWaitHandleDistributedLockHandle(@event);
                        }

                        // refresh the event in case it was abandoned by the original owner
                        RefreshEvent();
                    }
                }

                // fixed timeout: loop in abandonment check chunks
                var elapsedMillis = 0;
                do
                {
                    var nextWaitMillis = Math.Min(this._abandonmentCheckCadence.InMilliseconds, timeout.InMilliseconds - elapsedMillis);
                    if (await @event.WaitOneAsync(TimeSpan.FromMilliseconds(nextWaitMillis), cancellationToken).ConfigureAwait(false))
                    {
                        cleanup = false;
                        return new EventWaitHandleDistributedLockHandle(@event);
                    }

                    elapsedMillis += nextWaitMillis;

                    // refresh the event in case it was abandoned by the original owner
                    RefreshEvent();
                }
                while (elapsedMillis < timeout.InMilliseconds);

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

            void RefreshEvent()
            {
                @event.Dispose();
                @event = this.CreateEvent();
            }
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

            var @event = new EventWaitHandle(
                // if we create, start as unlocked
                initialState: true,
                // allow only one thread to hold the lock
                mode: EventResetMode.AutoReset,
                name: this.Name,
                createdNew: out _
            );
            @event.SetAccessControl(security);

            return @event;
        }

        bool IInternalDistributedLock<EventWaitHandleDistributedLockHandle>.WillGoAsync(TimeoutValue timeout, CancellationToken cancellationToken) => false;
    }
}
