using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Chaining;

public static class DistributedSemaphoreProviderExtensions
{
    /// <summary>
    /// Chains <paramref name="provider"/> with <paramref name="precedingProvider"/> such that locks created from the resulting provider are acquired first by <paramref name="precedingProvider"/>, then by <paramref name="provider"/>.
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="precedingProvider"></param>
    /// <returns></returns>
    public static IDistributedSemaphoreProvider PrecededBy(this IDistributedSemaphoreProvider provider, IDistributedSemaphoreProvider precedingProvider)
    {
        var providers = new List<IDistributedSemaphoreProvider>();
        if (precedingProvider is ChainedDistributedSemaphoreProvider chained)
        {
            providers.AddRange(chained.Providers);
        }
        else
        {
            providers.Add(precedingProvider);
        }
        if (provider is ChainedDistributedSemaphoreProvider chained2)
        {
            providers.AddRange(chained2.Providers);
        }
        else
        {
            providers.Add(provider);
        }
        return new ChainedDistributedSemaphoreProvider([.. providers]);
    }

    /// <summary>
    /// Chains <paramref name="provider"/> with <paramref name="followingProvider"/> such that locks created from the resulting provider are acquired first by <paramref name="provider"/>, then by <paramref name="followingProvider"/>.
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="followingProvider"></param>
    /// <returns></returns>
    public static IDistributedSemaphoreProvider FollowedBy(this IDistributedSemaphoreProvider provider, IDistributedSemaphoreProvider followingProvider)
    {
        var providers = new List<IDistributedSemaphoreProvider>();
        if (provider is ChainedDistributedSemaphoreProvider chained)
        {
            providers.AddRange(chained.Providers);
        }
        else
        {
            providers.Add(provider);
        }
        if (followingProvider is ChainedDistributedSemaphoreProvider chained2)
        {
            providers.AddRange(chained2.Providers);
        }
        else
        {
            providers.Add(followingProvider);
        }
        return new ChainedDistributedSemaphoreProvider([.. providers]);
    }
}
