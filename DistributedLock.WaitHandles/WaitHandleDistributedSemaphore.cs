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
        private readonly TimeoutValue _abandonmentCheckCadence;

        /// <summary>
        /// Constructs a lock with the given <paramref name="name"/>.
        /// 
        /// <paramref name="abandonmentCheckCadence"/> specifies how frequently we refresh our <see cref="Semaphore"/> object in case it is abandoned by
        /// its original owner. The default is 2s.
        /// 
        /// Unless <paramref name="exactName"/> is specified, <paramref name="name"/> will be escaped/hashed to ensure name validity.
        /// </summary>
        public WaitHandleDistributedSemaphore(string name, int maxCount, TimeSpan? abandonmentCheckCadence = null, bool exactName = false)
        {
            if (maxCount < 1) { throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "must be positive"); }

            this.Name = DistributedWaitHandleHelpers.ValidateAndFinalizeName(name, exactName);
            this.MaxCount = maxCount;
            this._abandonmentCheckCadence = DistributedWaitHandleHelpers.ValidateAndFinalizeAbandonmentCheckCadence(abandonmentCheckCadence);
        }

        /// <summary>
        /// Implements <see cref="IDistributedSemaphore.Name"/>
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Implements <see cref="IDistributedSemaphore.MaxCount"/>
        /// </summary>
        public int MaxCount { get; }

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
                var semaphore = new Semaphore(initialCount: this.MaxCount, maximumCount: this.MaxCount, name: this.Name, createdNew: out var createdNew);
                if (createdNew) { semaphore.SetAccessControl(security); }
                return semaphore;
            },
            tryOpenExisting: delegate (out Semaphore existing) { return Semaphore.TryOpenExisting(this.Name, out existing); }
        );
    }
}
