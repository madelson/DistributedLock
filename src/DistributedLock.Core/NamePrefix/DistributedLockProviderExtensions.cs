namespace Medallion.Threading.NamePrefix;

/// <summary>
/// Provides the <see cref="WithNamePrefix"/> extension method to <see cref="IDistributedLockProvider"/>.
/// </summary>
public static class DistributedLockProviderExtensions
{
    /// <summary>
    /// Creates a new <see cref="IDistributedLockProvider" /> that prepends <paramref name="namePrefix" /> to all lock names. This is useful for creating a "namespace" for locks, e.g. to avoid collisions between different applications using the same distributed lock provider.
    /// </summary>
    /// <param name="provider">The underlying <see cref="IDistributedLockProvider" />.</param>
    /// <param name="namePrefix">The prefix to prepend to all lock names.</param>
    /// <returns>A new <see cref="IDistributedLockProvider" /> that prepends <paramref name="namePrefix" /> to all lock names.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IDistributedLockProvider WithNamePrefix(this IDistributedLockProvider provider, string namePrefix)
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

        if (provider is NamePrefixedDistributedLockProvider prefixed)
        {
            // combine the key in advance to minimize indirection
            namePrefix = prefixed.NamePrefix + namePrefix;
            provider = prefixed.Inner;
        }

        return new NamePrefixedDistributedLockProvider(provider, namePrefix);
    }
}
