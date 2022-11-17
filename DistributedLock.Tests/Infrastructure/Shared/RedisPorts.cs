using System.Collections.Generic;
using System.Linq;

namespace Medallion.Threading.Tests;

internal static class RedisPorts
{
    // 6379 is the redis default, so don't use that
    public static readonly IReadOnlyList<int> DefaultPorts = Enumerable.Range(6380, count: 10).ToArray();
}
