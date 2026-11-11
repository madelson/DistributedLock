namespace Medallion.Threading.Azure;

/// <summary>
/// Selects an Azure table entity key (<c>PartitionKey</c> or <c>RowKey</c>) based on a lock's name.
/// Implicitly convertible from both <see cref="string"/> and <see cref="Func{String, String}"/>.
/// </summary>
public sealed class AzureTableKeySelector
{
    /// <summary>
    /// Placeholder substituted with the lock name when converting from a <see cref="string"/> template.
    /// </summary>
    public const string NamePlaceholder = "{name}";

    // Keys may not contain these characters, nor any control character.
    // https://learn.microsoft.com/en-us/rest/api/storageservices/understanding-the-table-service-data-model#characters-disallowed-in-key-fields
    private static readonly char[] DisallowedCharacters = { '/', '\\', '#', '?' };

    internal const int MaxKeyLength = 1024;

    private readonly Func<string, string> _selector;

    private AzureTableKeySelector(Func<string, string> selector)
    {
        this._selector = selector;
    }

    /// <summary>
    /// Creates a selector from a template. If the template contains <see cref="NamePlaceholder"/>, the lock
    /// name is substituted for it. Otherwise, the template is used verbatim as a constant key.
    /// </summary>
    public static AzureTableKeySelector FromTemplate(string template)
    {
        if (template == null) { throw new ArgumentNullException(nameof(template)); }

        return template.IndexOf(NamePlaceholder, StringComparison.Ordinal) >= 0
            ? new AzureTableKeySelector(name => template.Replace(NamePlaceholder, name))
            : new AzureTableKeySelector(_ => template);
    }

    /// <summary>
    /// Creates a selector from an arbitrary function of the lock name. The function must be deterministic:
    /// it is invoked once when the lock is constructed and the result is cached for the lock's lifetime.
    /// </summary>
    public static AzureTableKeySelector FromFunc(Func<string, string> selector) =>
        new(selector ?? throw new ArgumentNullException(nameof(selector)));

    /// <summary>
    /// Implicitly converts a template string via <see cref="FromTemplate(string)"/>.
    /// </summary>
    public static implicit operator AzureTableKeySelector(string template) => FromTemplate(template);

    /// <summary>
    /// Implicitly converts a function via <see cref="FromFunc(Func{string, string})"/>.
    /// </summary>
    public static implicit operator AzureTableKeySelector(Func<string, string> selector) => FromFunc(selector);

    /// <summary>
    /// Applies the selector and validates the result. Unlike the default key derivation, a user-supplied
    /// selector's output is never silently sanitized; an invalid key is an error.
    /// </summary>
    internal string SelectAndValidate(string lockName, string parameterName)
    {
        var key = this._selector(lockName)
            ?? throw new ArgumentException($"The {parameterName} selector returned null for lock name '{lockName}'.", parameterName);

        if (key.Length == 0)
        {
            throw new ArgumentException($"The {parameterName} selector returned an empty key for lock name '{lockName}'.", parameterName);
        }
        if (key.Length > MaxKeyLength)
        {
            throw new ArgumentException(
                $"The {parameterName} selector returned a key of length {key.Length} for lock name '{lockName}'; the maximum is {MaxKeyLength}.",
                parameterName);
        }

        var invalidIndex = key.IndexOfAny(DisallowedCharacters);
        if (invalidIndex < 0)
        {
            for (var i = 0; i < key.Length; ++i)
            {
                if (char.IsControl(key[i])) { invalidIndex = i; break; }
            }
        }
        if (invalidIndex >= 0)
        {
            throw new ArgumentException(
                $"The {parameterName} selector returned key '{key}' for lock name '{lockName}', which contains the disallowed character '{key[invalidIndex]}' at index {invalidIndex}. "
                    + $"Disallowed characters are {string.Join(", ", DisallowedCharacters)} and control characters.",
                parameterName);
        }

        return key;
    }
}