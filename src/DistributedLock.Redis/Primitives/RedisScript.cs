using Medallion.Threading.Redis.RedLock;
using StackExchange.Redis;
using System.Text.RegularExpressions;

namespace Medallion.Threading.Redis.Primitives;

internal class RedisScript<TArgument>(string script, Func<TArgument, object> parameters)
{
    private readonly LuaScript _script = LuaScript.Prepare(RemoveExtraneousWhitespace(script));

    public RedisResult Execute(IDatabase database, TArgument argument, bool fireAndForget = false) =>
        // database.ScriptEvaluate must be called instead of script.Evaluate in order to respect the database's key prefix
        database.ScriptEvaluate(this._script, parameters(argument), flags: RedLockHelper.GetCommandFlags(fireAndForget));

    public Task<RedisResult> ExecuteAsync(IDatabaseAsync database, TArgument argument, bool fireAndForget = false) =>
        // database.ScriptEvaluate must be called instead of script.Evaluate in order to respect the database's key prefix
        database.ScriptEvaluateAsync(this._script, parameters(argument), flags: RedLockHelper.GetCommandFlags(fireAndForget));

    // send the smallest possible script to the server
    private static string RemoveExtraneousWhitespace(string script) => Regex.Replace(script.Trim(), @"\s+", " ");
}
