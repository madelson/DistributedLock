using Medallion.Threading.Redis.Primitives;
using NUnit.Framework;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Medallion.Threading.Tests.Redis;

[Category("CI")]
public class RedisLibraryTest
{
    // ensures that we are caching the preparation of these
    [Test]
    public void TestAllRedisScriptFieldsAreStatic()
    {
        var redisScriptFields = typeof(RedisScript<>).Assembly
            .GetTypes()
            .Where(t => t.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
            .SelectMany(t => t.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            .Where(f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(RedisScript<>));
        foreach (var field in redisScriptFields)
        {
            Assert.IsTrue(field.IsStatic, field.ToString());
        }
    }
}
