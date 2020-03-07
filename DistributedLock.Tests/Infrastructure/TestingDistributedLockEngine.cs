using NUnit.Framework;
using System;

namespace Medallion.Threading.Tests
{
    public abstract class TestingDistributedLockEngine : ActionRegistrationDisposable
    {
        private readonly string _currentTestFullName = TestContext.CurrentContext.Test.FullName;

        internal IDistributedLock CreateLock(string baseName) =>
            this.CreateLockWithExactName(this.GetUniqueSafeLockName(baseName));

        /// <summary>
        /// Returns a lock name based on <paramref name="baseName"/> which is "namespaced" by the current
        /// test and framework name, thus avoiding potential collisions between test cases
        /// </summary>
        internal string GetUniqueSafeLockName(string baseName = "") =>
            this.GetSafeName($"{baseName}_{this._currentTestFullName}_{TestHelper.FrameworkName}");

        internal abstract IDistributedLock CreateLockWithExactName(string name);

        internal abstract string GetSafeName(string name);
        
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
