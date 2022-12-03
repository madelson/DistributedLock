using Medallion.Threading.Internal;

namespace Medallion.Threading.Redis.RedLock;

internal readonly struct RedLockTimeouts
{
    public RedLockTimeouts(
        TimeoutValue expiry,
        TimeoutValue minValidityTime)
    {
        this.Expiry = expiry;
        this.MinValidityTime = minValidityTime;
    }

    public TimeoutValue Expiry { get; }
    public TimeoutValue MinValidityTime { get; }
    public TimeoutValue AcquireTimeout => this.Expiry.TimeSpan - this.MinValidityTime.TimeSpan;
}
