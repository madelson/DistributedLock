using Medallion.Threading.Redis.RedLock;
using StackExchange.Redis;
using System.Text.RegularExpressions;

namespace Medallion.Threading.Redis.Primitives;

internal class RedisScript<TArgument>
{
    private readonly LuaScript _script;
    private readonly Func<TArgument, object> _parameters;

    public RedisScript(string script, Func<TArgument, object> parameters)
    {
        this._script = LuaScript.Prepare(RemoveExtraneousWhitespace(script));
        this._parameters = parameters;
    }

    public RedisResult Execute(IDatabase database, TArgument argument, bool fireAndForget = false) =>
        // database.ScriptEvaluate must be called instead of _script.Evaluate in order to respect the database's key prefix
        database.ScriptEvaluate(this._script, this._parameters(argument), flags: RedLockHelper.GetCommandFlags(fireAndForget));

    public Task<RedisResult> ExecuteAsync(IDatabaseAsync database, TArgument argument, bool fireAndForget = false) =>
        // database.ScriptEvaluate must be called instead of _script.Evaluate in order to respect the database's key prefix
        database.ScriptEvaluateAsync(this._script, this._parameters(argument), flags: RedLockHelper.GetCommandFlags(fireAndForget));

    // send the smallest possible script to the server
    private static string RemoveExtraneousWhitespace(string script) => Regex.Replace(script.Trim(), @"\s+", " ");
}
