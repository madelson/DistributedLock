namespace Medallion.Threading.Tests;

internal static class TargetFramework
{
    public const string Current =
#if NET471
            "net471";
#elif NETCOREAPP3_1
            "netcoreapp3.1";
#endif
}
