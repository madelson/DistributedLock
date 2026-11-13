namespace Medallion.Threading.NamePrefix;

/// <summary>
/// Provides the <see cref="WithNamePrefix"/> extension method to <see cref="IDistributedSemaphoreProvider"/>.
/// </summary>
public static class DistributedSemaphoreProviderExtensions
{
    /// <summary>
    /// Creates a new <see cref="IDistributedSemaphoreProvider" /> that prepends <paramref name="namePrefix" /> to all semaphore names. This is useful for creating a "namespace" for semaphores, e.g. to avoid collisions between different applications using the same distributed semaphore provider.
    /// </summary>
    /// <param name="provider">The underlying <see cref="IDistributedSemaphoreProvider" />.</param>
    /// <param name="namePrefix">The prefix to prepend to all semaphore names.</param>
    /// <returns>A new <see cref="IDistributedSemaphoreProvider" /> that prepends <paramref name="namePrefix" /> to all semaphore names.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IDistributedSemaphoreProvider WithNamePrefix(this IDistributedSemaphoreProvider provider, string namePrefix)
    {
        if (provider == null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        if (namePrefix == null)
        {
            throw new ArgumentNullException(nameof(namePrefix));
        }

        if (namePrefix == string.Empty)
        {
            return provider;
        }

        if (provider is NamePrefixedDistributedSemaphoreProvider prefixed)
        {
            // combine the key in advance to minimize indirection
            namePrefix = prefixed.NamePrefix + namePrefix;
            provider = prefixed.Inner;
        }

        return new NamePrefixedDistributedSemaphoreProvider(provider, namePrefix);
    }
}
