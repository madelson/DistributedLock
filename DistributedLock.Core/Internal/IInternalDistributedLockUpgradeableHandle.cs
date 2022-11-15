using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal;

#if DEBUG
public
#else
internal
#endif
interface IInternalDistributedLockUpgradeableHandle : IDistributedLockUpgradeableHandle
{
    ValueTask<bool> InternalTryUpgradeToWriteLockAsync(TimeoutValue timeout, CancellationToken cancellationToken);
}
