namespace Medallion.Threading.Azure;

/// <summary>
/// Selects an <see cref="AzureTableAcquireStrategy"/> before each acquire attempt. Implicitly
/// convertible from both <see cref="AzureTableAcquireStrategy"/> and a function of
/// <see cref="AzureTableAcquireContext"/>.
/// </summary>
public sealed class AzureTableAcquireStrategySelector
{
    /// <summary>
    /// The default policy: insert-first until contention is observed, then read-first for the
    /// remainder of the current acquire.
    /// </summary>
    public static AzureTableAcquireStrategySelector Adaptive { get; } =
        new(context => context.RowExists == true
            ? AzureTableAcquireStrategy.ReadFirst
            : AzureTableAcquireStrategy.InsertFirst);

    private readonly Func<AzureTableAcquireContext, AzureTableAcquireStrategy> _selector;

    private AzureTableAcquireStrategySelector(Func<AzureTableAcquireContext, AzureTableAcquireStrategy> selector)
    {
        this._selector = selector;
    }

    /// <summary>
    /// Creates a selector which always returns <paramref name="strategy"/>.
    /// </summary>
    public static AzureTableAcquireStrategySelector FromConstant(AzureTableAcquireStrategy strategy)
    {
        ValidateStrategy(strategy, nameof(strategy));
        return new(_ => strategy);
    }

    /// <summary>
    /// Creates a selector from a function which is invoked before every acquire attempt. The function
    /// should be inexpensive; any exception it throws will propagate out of the acquire call.
    /// </summary>
    public static AzureTableAcquireStrategySelector FromFunc(Func<AzureTableAcquireContext, AzureTableAcquireStrategy> selector) =>
        new(selector ?? throw new ArgumentNullException(nameof(selector)));

    /// <summary>
    /// Implicitly converts a constant strategy via <see cref="FromConstant(AzureTableAcquireStrategy)"/>.
    /// </summary>
    public static implicit operator AzureTableAcquireStrategySelector(AzureTableAcquireStrategy strategy) => FromConstant(strategy);

    /// <summary>
    /// Implicitly converts a function via <see cref="FromFunc(Func{AzureTableAcquireContext, AzureTableAcquireStrategy})"/>.
    /// </summary>
    public static implicit operator AzureTableAcquireStrategySelector(Func<AzureTableAcquireContext, AzureTableAcquireStrategy> selector) => FromFunc(selector);

    internal AzureTableAcquireStrategy Select(AzureTableAcquireContext context)
    {
        var strategy = this._selector(context);
        ValidateStrategy(strategy, parameterName: null);
        return strategy;
    }

    private static void ValidateStrategy(AzureTableAcquireStrategy strategy, string? parameterName)
    {
        if (strategy is not (AzureTableAcquireStrategy.InsertFirst or AzureTableAcquireStrategy.ReadFirst))
        {
            throw parameterName != null
                ? new ArgumentOutOfRangeException(parameterName, strategy, "undefined value")
                : new InvalidOperationException($"{nameof(AzureTableAcquireStrategySelector)} returned undefined value {strategy}");
        }
    }
}
