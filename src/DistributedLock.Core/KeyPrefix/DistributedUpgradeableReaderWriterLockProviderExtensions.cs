namespace Medallion.Threading.KeyPrefix;

/// <summary>
/// Provides the <see cref="WithKeyPrefix"/> extension method to <see cref="IDistributedUpgradeableReaderWriterLockProvider"/>.
/// </summary>
public static class DistributedUpgradeableReaderWriterLockProviderExtensions
{
    /// <summary>
    /// Creates a new <see cref="IDistributedUpgradeableReaderWriterLockProvider" /> that prepends <paramref name="keyPrefix" /> to all lock names. This is useful for creating a "namespace" for locks, e.g. to avoid collisions between different applications using the same distributed lock provider.
    /// </summary>
    /// <param name="provider">The underlying <see cref="IDistributedUpgradeableReaderWriterLockProvider" />.</param>
    /// <param name="keyPrefix">The prefix to prepend to all lock names.</param>
    /// <returns>A new <see cref="IDistributedUpgradeableReaderWriterLockProvider" /> that prepends <paramref name="keyPrefix" /> to all lock names.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IDistributedUpgradeableReaderWriterLockProvider WithKeyPrefix(this IDistributedUpgradeableReaderWriterLockProvider provider, string keyPrefix)
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

        if (provider is KeyPrefixedDistributedUpgradeableReaderWriterLockProvider prefixed)
        {
            // combine the key in advance to minimize indirection
            keyPrefix = prefixed.KeyPrefix + keyPrefix;
            provider = prefixed.Inner;
        }

        return new KeyPrefixedDistributedUpgradeableReaderWriterLockProvider(provider, keyPrefix);
    }
}
