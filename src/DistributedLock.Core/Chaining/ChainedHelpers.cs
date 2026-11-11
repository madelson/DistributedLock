namespace Medallion.Threading.Chaining;

internal static class ChainedHelpers
{
    public static T[] Validate<T>(T[] primitives, string paramName)
        where T : class
    {
        if (primitives is null) { throw new ArgumentNullException(paramName); }
        if (primitives.Length == 0) { throw new ArgumentException("At least one underlying synchronization primitive is required.", paramName); }
        if (Array.IndexOf(primitives, null) >= 0) { throw new ArgumentException("Must not contain null", paramName); }

        return primitives;
    }
}
