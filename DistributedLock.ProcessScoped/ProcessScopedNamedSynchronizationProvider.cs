using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading
{
    /// <summary>
    /// Implements <see cref="IDistributedLockProvider"/> for <see cref="ProcessScopedNamedLock"/>,
    /// <see cref="IDistributedUpgradeableReaderWriterLockProvider"/> for <see cref="ProcessScopedNamedReaderWriterLock"/>,
    /// and <see cref="IDistributedSemaphoreProvider"/> for <see cref="ProcessScopedNamedSemaphore"/>.
    /// 
    /// Note that these implementations are SCOPED TO JUST THE CURRENT PROCESS and therefore are NOT TRULY DISTRIBUTED. 
    /// Therefore, they is intended primarily for testing or scenarios where name-based locking is useful (e.g. when frequently 
    /// creating and destroying fine-grained locks).
    /// </summary>
    public sealed class ProcessScopedNamedSynchronizationProvider : IDistributedLockProvider, IDistributedUpgradeableReaderWriterLockProvider, IDistributedSemaphoreProvider
    {
        /// <summary>
        /// Constructs a <see cref="ProcessScopedNamedLock"/> with the provided <paramref name="name"/>.
        /// </summary>
        public ProcessScopedNamedLock CreateLock(string name) => new(name);

        IDistributedLock IDistributedLockProvider.CreateLock(string name) => this.CreateLock(name);

        /// <summary>
        /// Constructs a <see cref="ProcessScopedNamedReaderWriterLock"/> with the provided <paramref name="name"/>.
        /// </summary>
        public ProcessScopedNamedReaderWriterLock CreateReaderWriterLock(string name) => new(name);

        IDistributedReaderWriterLock IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string name) => 
            this.CreateReaderWriterLock(name);

        IDistributedUpgradeableReaderWriterLock IDistributedUpgradeableReaderWriterLockProvider.CreateUpgradeableReaderWriterLock(string name) =>
            this.CreateReaderWriterLock(name);

        /// <summary>
        /// Constructs a <see cref="ProcessScopedNamedSemaphore"/> with the provided <paramref name="name"/> and <paramref name="maxCount"/>.
        /// </summary>
        public ProcessScopedNamedSemaphore CreateSemaphore(string name, int maxCount) => new(name, maxCount);

        IDistributedSemaphore IDistributedSemaphoreProvider.CreateSemaphore(string name, int maxCount) => this.CreateSemaphore(name, maxCount);
    }
}
