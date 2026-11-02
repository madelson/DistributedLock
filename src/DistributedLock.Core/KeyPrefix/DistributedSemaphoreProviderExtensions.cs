namespace Medallion.Threading.KeyPrefix;

/// <summary>
/// Provides the <see cref="WithKeyPrefix"/> extension method to <see cref="IDistributedSemaphoreProvider"/>.
/// </summary>
public static class DistributedSemaphoreProviderExtensions
{
    /// <summary>
    /// Creates a new <see cref="IDistributedSemaphoreProvider" /> that prepends <paramref name="keyPrefix" /> to all semaphore names. This is useful for creating a "namespace" for semaphores, e.g. to avoid collisions between different applications using the same distributed semaphore provider.
    /// </summary>
    /// <param name="provider">The underlying <see cref="IDistributedSemaphoreProvider" />.</param>
    /// <param name="keyPrefix">The prefix to prepend to all semaphore names.</param>
    /// <returns>A new <see cref="IDistributedSemaphoreProvider" /> that prepends <paramref name="keyPrefix" /> to all semaphore names.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IDistributedSemaphoreProvider WithKeyPrefix(this IDistributedSemaphoreProvider provider, string keyPrefix)
    {
        if (provider == null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        if (keyPrefix == null)
        {
            throw new ArgumentNullException(nameof(keyPrefix));
        }

        if (keyPrefix == string.Empty)
        {
            return provider;
        }

        if (provider is KeyPrefixedDistributedSemaphoreProvider prefixed)
        {
            // combine the key in advance to minimize indirection
            keyPrefix = prefixed.KeyPrefix + keyPrefix;
            provider = prefixed.Inner;
        }

        return new KeyPrefixedDistributedSemaphoreProvider(provider, keyPrefix);
    }
}
