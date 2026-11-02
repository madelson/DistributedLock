namespace Medallion.Threading.KeyPrefix;

/// <summary>
/// Provides the <see cref="WithKeyPrefix"/> extension method to <see cref="IDistributedLockProvider"/>.
/// </summary>
public static class DistributedLockProviderExtensions
{
    /// <summary>
    /// Creates a new <see cref="IDistributedLockProvider" /> that prepends <paramref name="keyPrefix" /> to all lock names. This is useful for creating a "namespace" for locks, e.g. to avoid collisions between different applications using the same distributed lock provider.
    /// </summary>
    /// <param name="provider">The underlying <see cref="IDistributedLockProvider" />.</param>
    /// <param name="keyPrefix">The prefix to prepend to all lock names.</param>
    /// <returns>A new <see cref="IDistributedLockProvider" /> that prepends <paramref name="keyPrefix" /> to all lock names.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IDistributedLockProvider WithKeyPrefix(this IDistributedLockProvider provider, string keyPrefix)
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

        if (provider is KeyPrefixedDistributedLockProvider prefixed)
        {
            // combine the key in advance to minimize indirection
            keyPrefix = prefixed.KeyPrefix + keyPrefix;
            provider = prefixed.Inner;
        }

        return new KeyPrefixedDistributedLockProvider(provider, keyPrefix);
    }
}
