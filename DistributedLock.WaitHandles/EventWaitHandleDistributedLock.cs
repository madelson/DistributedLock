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
            this.Name = DistributedWaitHandleHelpers.ValidateAndFinalizeName(name, exactName);
            this._abandonmentCheckCadence = DistributedWaitHandleHelpers.ValidateAndFinalizeAbandonmentCheckCadence(abandonmentCheckCadence);
        }

        /// <summary>
        /// The maximum allowed length for lock names
        /// </summary>
        public static int MaxNameLength => DistributedWaitHandleHelpers.MaxNameLength;

        /// <summary>
        /// Implements <see cref="IDistributedLock.Name"/>
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// TODO probably remove this API
        /// </summary>
        public static string GetSafeName(string name) => DistributedWaitHandleHelpers.GetSafeName(name);

        async ValueTask<EventWaitHandleDistributedLockHandle?> IInternalDistributedLock<EventWaitHandleDistributedLockHandle>.InternalTryAcquireAsync(
            TimeoutValue timeout,
            CancellationToken cancellationToken)
        {
            var @event = await DistributedWaitHandleHelpers.CreateAndWaitAsync(
                createHandle: this.CreateEvent,
                abandonmentCheckCadence: this._abandonmentCheckCadence,
                timeout: timeout,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            return @event != null ? new EventWaitHandleDistributedLockHandle(@event) : null;
        }

        private EventWaitHandle CreateEvent() => DistributedWaitHandleHelpers.CreateDistributedWaitHandle(
            createNew: () =>
            {
                // based on http://stackoverflow.com/questions/2590334/creating-a-cross-process-eventwaithandle
                var security = new EventWaitHandleSecurity();
                // allow anyone to wait on and signal this lock
                security.AddAccessRule(new EventWaitHandleAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, domainSid: null),
                    EventWaitHandleRights.FullControl, // doesn't seem to work without this :-/
                    AccessControlType.Allow
                ));
                var @event = new EventWaitHandle(
                    // if we create, start as unlocked
                    initialState: true,
                    // allow only one thread to hold the lock
                    mode: EventResetMode.AutoReset,
                    name: this.Name,
                    createdNew: out var createdNew
                );
                if (createdNew) { @event.SetAccessControl(security); }
                return @event;
            },
            tryOpenExisting: delegate (out EventWaitHandle existing) { return EventWaitHandle.TryOpenExisting(this.Name, out existing); }
        );
    }
}
