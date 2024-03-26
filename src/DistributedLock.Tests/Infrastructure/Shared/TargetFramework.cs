namespace Medallion.Threading.Tests;

internal static class TargetFramework
{
    public const string Current =
#if NET472
            "net472";
#elif NET8_0
            "net8.0";
#endif
}
