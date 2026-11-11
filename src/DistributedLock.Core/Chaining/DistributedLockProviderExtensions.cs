namespace Medallion.Threading.Chaining;

public static class DistributedLockProviderExtensions
{

    /// <summary>
    /// Chains <paramref name="provider"/> with <paramref name="precedingProvider"/> such that locks created from the resulting provider are acquired first by <paramref name="precedingProvider"/> then by <paramref name="provider"/>.
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="precedingProvider"></param>
    /// <returns></returns>
    public static IDistributedLockProvider PrecededBy(this IDistributedLockProvider provider, IDistributedLockProvider precedingProvider)
    {
        var providers = new List<IDistributedLockProvider>();
        if (precedingProvider is ChainedDistributedLockProvider chained)
        {
            providers.AddRange(chained.Providers);
        }
        else
        {
            providers.Add(precedingProvider);
        }
        if (provider is ChainedDistributedLockProvider chained2)
        {
            providers.AddRange(chained2.Providers);
        }
        else
        {
            providers.Add(provider);
        }
        return new ChainedDistributedLockProvider(lockProviders: [.. providers]);
    }

    /// <summary>
    /// Chains <paramref name="provider"/> with <paramref name="followingProvider"/> such that locks created from the resulting provider are acquired first by <paramref name="provider"/>, then by <paramref name="followingProvider"/>.
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="followingProvider"></param>
    /// <returns></returns>
    public static IDistributedLockProvider FollowedBy(this IDistributedLockProvider provider, IDistributedLockProvider followingProvider)
    {
        var providers = new List<IDistributedLockProvider>();
        if (provider is ChainedDistributedLockProvider chained)
        {
            providers.AddRange(chained.Providers);
        }
        else
        {
            providers.Add(provider);
        }
        if (followingProvider is ChainedDistributedLockProvider chained2)
        {
            providers.AddRange(chained2.Providers);
        }
        else
        {
            providers.Add(followingProvider);
        }
        return new ChainedDistributedLockProvider([.. providers]);
    }
}
