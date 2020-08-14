using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    /// <summary>
    /// A <see cref="IDistributedLockHandle"/> that can be upgraded to a write lock
    /// </summary>
    public interface IDistributedLockUpgradeableHandle : IDistributedLockHandle
    {
        /// <summary>
        /// Attempts to upgrade a WRITE lock synchronously. Not compatible with another WRITE lock or a UPGRADE lock
        /// </summary>
        bool TryUpgradeToWriteLock(TimeSpan timeout = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upgrades to a WRITE lock synchronously. Not compatible with another WRITE lock or a UPGRADE lock
        /// </summary>
        void UpgradeToWriteLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to upgrade a WRITE lock asynchronously. Not compatible with another WRITE lock or a UPGRADE lock
        /// </summary>
        ValueTask<bool> TryUpgradeToWriteLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upgrades to a WRITE lock asynchronously. Not compatible with another WRITE lock or a UPGRADE lock
        /// </summary>
        ValueTask UpgradeToWriteLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
    }
}
