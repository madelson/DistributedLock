using Medallion.Threading.Internal;
using System;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.WaitHandles
{
    /// <summary>
    /// A distributed lock based on a global <see cref="EventWaitHandle"/> on Windows.
    /// </summary>
    public sealed partial class EventWaitHandleDistributedLock : IInternalDistributedLock<EventWaitHandleDistributedLockHandle>
    {
        internal const string GlobalPrefix = @"Global\";
        private static readonly TimeoutValue DefaultAbandonmentCheckCadence = TimeSpan.FromSeconds(2);

        private readonly TimeoutValue _abandonmentCheckCadence;

        /// <summary>
        /// Constructs a lock with the given <paramref name="name"/>.
        /// 
        /// <paramref name="abandonmentCheckCadence"/> specifies how frequently we refresh our <see cref="EventWaitHandle"/> object in case it is abandoned by
        /// its original owner. The default is 2s.
        /// 
        /// Unless <paramref name="exactName"/> is specified, <see cref="GetSafeName(string)"/> will be called on the provided <paramref name="name"/>.
        /// </summary>
        public EventWaitHandleDistributedLock(string name, TimeSpan? abandonmentCheckCadence = null, bool exactName = false)
        {
            if (exactName)
            {
                if (name == null) { throw new ArgumentNullException(nameof(name)); }

                if (name.Length > MaxNameLength) { throw new FormatException($"{nameof(name)}: must be at most {MaxNameLength} characters"); }
                if (!name.StartsWith(GlobalPrefix, StringComparison.Ordinal)) { throw new FormatException($"{nameof(name)}: must start with '{GlobalPrefix}'"); }
                if (name == GlobalPrefix) { throw new FormatException($"{nameof(name)} must not be exactly '{GlobalPrefix}'"); }
                if (name.IndexOf('\\', startIndex: GlobalPrefix.Length) >= 0) { throw new FormatException(nameof(name) + @": must not contain '\'"); }

                this.Name = name;
            }
            else
            {
                this.Name = GetSafeName(name);
            }

            if (abandonmentCheckCadence.HasValue)
            {
                this._abandonmentCheckCadence = new TimeoutValue(abandonmentCheckCadence, nameof(abandonmentCheckCadence));
                if (this._abandonmentCheckCadence.IsZero) { throw new ArgumentOutOfRangeException(nameof(abandonmentCheckCadence), "must not be zero"); }
            }
            else { this._abandonmentCheckCadence = DefaultAbandonmentCheckCadence; }
        }

        /// <summary>
        /// The maximum allowed length for lock names
        /// </summary>
        // 260 based on LINQPad experimentation
        public static int MaxNameLength => 260;

        /// <summary>
        /// Implements <see cref="IDistributedLock.Name"/>
        /// </summary>
        public string Name { get; }

        bool IDistributedLock.IsReentrant => false;

        /// <summary>
        /// Equivalent to <see cref="IDistributedLockProvider.GetSafeLockName(string)"/>
        /// </summary>
        public static string GetSafeName(string name)
        {
            if (name == null) { throw new ArgumentNullException(nameof(name)); }

            // Note: the reason we don't add GlobalPrefix inside the ToSafeLockName callback
            // is for backwards compat with the SystemDistributedLock.GetSafeLockName in 1.0.
            // In that version, the global prefix was not exposed as part of the name, and as
            // such it was not accounted for in the hashing performed by ToSafeLockName.

            if (name.StartsWith(GlobalPrefix, StringComparison.Ordinal))
            {
                var suffix = name.Substring(GlobalPrefix.Length);
                var safeSuffix = ConvertToSafeSuffix(suffix);
                return safeSuffix == suffix ? name : GlobalPrefix + safeSuffix;
            }

            return GlobalPrefix + ConvertToSafeSuffix(name);

            static string ConvertToSafeSuffix(string suffix) => DistributedLockHelpers.ToSafeName(
                suffix,
                MaxNameLength - GlobalPrefix.Length,
                s => s.Length == 0 ? "EMPTY" : s.Replace('\\', '_')
            );
        }

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
            var security = new EventWaitHandleSecurity();
            // allow anyone to wait on and signal this lock
            security.AddAccessRule(
                new EventWaitHandleAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, domainSid: null),
                    EventWaitHandleRights.FullControl, // doesn't seem to work without this :-/
                    AccessControlType.Allow
                )
            );

            const int MaxTries = 3;
            var tries = 0;
            
            while (true)
            {
                ++tries;
                try
                {
                    // based on http://stackoverflow.com/questions/2590334/creating-a-cross-process-eventwaithandle
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
                // fallback handling based on https://stackoverflow.com/questions/1784392/my-eventwaithandle-says-access-to-the-path-is-denied-but-its-not
                catch (UnauthorizedAccessException) when (tries <= MaxTries)
                {
                    if (EventWaitHandle.TryOpenExisting(this.Name, out var existing))
                    {
                        return existing;
                    }
                }

                // if we fail both, we might be in a race. Add in a small random sleep to attempt desynchronization
                Thread.Sleep(new Random(Guid.NewGuid().GetHashCode()).Next(10 * tries));
            }
        }

        bool IInternalDistributedLock<EventWaitHandleDistributedLockHandle>.WillGoAsync(TimeoutValue timeout, CancellationToken cancellationToken) => false;
    }
}
