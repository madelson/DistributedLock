using Medallion.Threading.Redis.RedLock;
using StackExchange.Redis;
using System.Text.RegularExpressions;

namespace Medallion.Threading.Redis.Primitives;

internal class RedisScript<TArgument>(string script, Func<TArgument, (RedisKey[], RedisValue[])> parameters)
{
    private readonly string _script = RemoveExtraneousWhitespace(script);

    public RedisResult Execute(IDatabase database, TArgument argument, bool fireAndForget = false)
    {
        var (keys, values) = parameters(argument);

        // database.ScriptEvaluate must be called instead of script.Evaluate in order to respect the database's key prefix
        return database.ScriptEvaluate(this._script, keys, values, RedLockHelper.GetCommandFlags(fireAndForget));
    }

    public Task<RedisResult> ExecuteAsync(IDatabaseAsync database, TArgument argument, bool fireAndForget = false)
    {
        var (keys, values) = parameters(argument);

        // database.ScriptEvaluate must be called instead of script.Evaluate in order to respect the database's key prefix
        return database.ScriptEvaluateAsync(this._script, keys, values, RedLockHelper.GetCommandFlags(fireAndForget));
    }

    // send the smallest possible script to the server
    private static string RemoveExtraneousWhitespace(string script) => Regex.Replace(script.Trim(), @"\s+", " ");
}
