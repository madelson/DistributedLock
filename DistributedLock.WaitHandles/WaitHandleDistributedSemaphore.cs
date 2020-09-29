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
    /// <summary>
    /// Implements a distributed semaphore based on a global <see cref="Semaphore"/>
    /// </summary>
    public sealed partial class WaitHandleDistributedSemaphore : IInternalDistributedSemaphore<WaitHandleDistributedSemaphoreHandle>
    {
        private readonly int _maxCount;
        private readonly TimeoutValue _abandonmentCheckCadence;

        /// <summary>
        /// Constructs a lock with the given <paramref name="name"/>.
        /// 
        /// <paramref name="abandonmentCheckCadence"/> specifies how frequently we refresh our <see cref="Semaphore"/> object in case it is abandoned by
        /// its original owner. The default is 2s.
        /// 
        /// Unless <paramref name="exactName"/> is specified, <see cref="GetSafeName(string)"/> will be called on the provided <paramref name="name"/>.
        /// </summary>
        public WaitHandleDistributedSemaphore(string name, int maxCount, TimeSpan? abandonmentCheckCadence = null, bool exactName = false)
        {
            if (maxCount < 1) { throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "must be positive"); }

            this.Name = DistributedWaitHandleHelpers.ValidateAndFinalizeName(name, exactName);
            this._maxCount = maxCount;
            this._abandonmentCheckCadence = DistributedWaitHandleHelpers.ValidateAndFinalizeAbandonmentCheckCadence(abandonmentCheckCadence);
        }

        /// <summary>
        /// The maximum length allowed for semaphore names
        /// </summary>
        public static int MaxNameLength => DistributedWaitHandleHelpers.MaxNameLength;

        /// <summary>
        /// Returns either the provided <paramref name="name"/> or a transformed version of <paramref name="name"/> which
        /// is safe to use with the "exactName" constructor parameter.
        /// </summary>
        public static string GetSafeName(string name) => DistributedWaitHandleHelpers.GetSafeName(name);

        /// <summary>
        /// The semaphore name
        /// </summary>
        public string Name { get; }

        async ValueTask<WaitHandleDistributedSemaphoreHandle?> IInternalDistributedSemaphore<WaitHandleDistributedSemaphoreHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
        {
            var semaphore = await DistributedWaitHandleHelpers.CreateAndWaitAsync(
                createHandle: this.CreateSemaphore,
                abandonmentCheckCadence: this._abandonmentCheckCadence,
                timeout: timeout,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            return semaphore != null ? new WaitHandleDistributedSemaphoreHandle(semaphore) : null;
        }

        private Semaphore CreateSemaphore() => DistributedWaitHandleHelpers.CreateDistributedWaitHandle(
            createNew: () =>
            {
                var security = new SemaphoreSecurity();
                // allow anyone to wait on and signal this semaphore
                security.AddAccessRule(new SemaphoreAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, domainSid: null),
                    SemaphoreRights.FullControl,
                    AccessControlType.Allow
                ));
                var semaphore = new Semaphore(initialCount: this._maxCount, maximumCount: this._maxCount, name: this.Name, createdNew: out var createdNew);
                if (createdNew) { semaphore.SetAccessControl(security); }
                return semaphore;
            },
            tryOpenExisting: delegate (out Semaphore existing) { return Semaphore.TryOpenExisting(this.Name, out existing); }
        );
    }
}
