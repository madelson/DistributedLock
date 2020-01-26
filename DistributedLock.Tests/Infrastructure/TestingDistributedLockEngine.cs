using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    public abstract class TestingDistributedLockEngine : ActionRegistrationDisposable
    {
        private readonly string _currentTestType = TestHelper.CurrentTestType!.Name;

        internal IDistributedLock CreateLock(string name)
        {
            return this.CreateLockWithExactName(this.GetSafeLockName(name + this._currentTestType));
        }

        internal abstract IDistributedLock CreateLockWithExactName(string name);

        internal abstract string GetSafeLockName(string name);
        
        /// <summary>
        /// Performs any additional cleanup beyond basic garbage collection needed to have a lock
        /// released via abandoning its handle
        /// </summary>
        internal virtual void PerformCleanupForLockAbandonment() { }

        /// <summary>
        /// Determines whether acquiring the same lock again will be successful
        /// </summary>
        internal abstract bool IsReentrant { get; }

        /// <summary>
        /// Identifier passed to DistributedLockTaker.exe to construct the right type of lock
        /// </summary>
        internal virtual string CrossProcessLockType => this.CreateLock(Guid.NewGuid().ToString()).GetType().Name;
    }
}
