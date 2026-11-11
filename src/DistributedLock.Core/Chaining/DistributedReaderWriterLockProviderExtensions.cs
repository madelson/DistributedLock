namespace Medallion.Threading.Chaining;

public static class DistributedReaderWriterLockProviderExtensions
{

    /// <summary>
    /// Chains <paramref name="provider"/> with <paramref name="precedingProvider"/> such that locks created from the resulting provider are acquired first by <paramref name="precedingProvider"/> then by <paramref name="provider"/>.
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="precedingProvider"></param>
    /// <returns></returns>
    public static IDistributedReaderWriterLockProvider PrecededBy(this IDistributedReaderWriterLockProvider provider, IDistributedReaderWriterLockProvider precedingProvider)
    {
        var providers = new List<IDistributedReaderWriterLockProvider>();
        if (precedingProvider is ChainedDistributedReaderWriterLockProvider chained)
        {
            providers.AddRange(chained.Providers);
        }
        else
        {
            providers.Add(precedingProvider);
        }
        if (provider is ChainedDistributedReaderWriterLockProvider chained2)
        {
            providers.AddRange(chained2.Providers);
        }
        else
        {
            providers.Add(provider);
        }
        return new ChainedDistributedReaderWriterLockProvider(lockProviders: [.. providers]);
    }

    /// <summary>
    /// Chains <paramref name="provider"/> with <paramref name="followingProvider"/> such that locks created from the resulting provider are acquired first by <paramref name="provider"/>, then by <paramref name="followingProvider"/>.
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="followingProvider"></param>
    /// <returns></returns>
    public static IDistributedReaderWriterLockProvider FollowedBy(this IDistributedReaderWriterLockProvider provider, IDistributedReaderWriterLockProvider followingProvider)
    {
        var providers = new List<IDistributedReaderWriterLockProvider>();
        if (provider is ChainedDistributedReaderWriterLockProvider chained)
        {
            providers.AddRange(chained.Providers);
        }
        else
        {
            providers.Add(provider);
        }
        if (followingProvider is ChainedDistributedReaderWriterLockProvider chained2)
        {
            providers.AddRange(chained2.Providers);
        }
        else
        {
            providers.Add(followingProvider);
        }
        return new ChainedDistributedReaderWriterLockProvider([.. providers]);
    }
}
