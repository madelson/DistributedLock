using Medallion.Threading.Internal;
using Medallion.Threading.Redis.RedLock;
using StackExchange.Redis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Medallion.Threading.Redis.Primitives;

/// <summary>
/// We use this class over Redis's <see cref="LuaScript"/> because that class's parameters all get mapped to ARGV[..] and we want
/// to appropriately map key parameters to KEYS[..] for compatibility with some cloud scenarios. See #254
/// </summary>
internal readonly struct RedisScript<TArg>
{
    private readonly string _script;
    private readonly Func<TArg, RedisKey[]> _getKeys;
    private readonly Func<TArg, RedisValue[]> _getValues;

    public RedisScript(RedisScriptInterpolatedString scriptTemplate)
    {
        (this._script, this._getKeys, this._getValues) = scriptTemplate.ToScript();
    }

    public RedisResult Execute(IDatabase database, TArg arg, bool fireAndForget = false) =>
        database.ScriptEvaluate(this._script, this._getKeys(arg), this._getValues(arg), flags: RedLockHelper.GetCommandFlags(fireAndForget));

    public Task<RedisResult> ExecuteAsync(IDatabaseAsync database, TArg arg, bool fireAndForget = false) =>
        database.ScriptEvaluateAsync(this._script, this._getKeys(arg), this._getValues(arg), flags: RedLockHelper.GetCommandFlags(fireAndForget));

    public static RedisScriptInterpolatedString Fragment(RedisScriptInterpolatedString fragment) => fragment;

    [InterpolatedStringHandler]
    internal readonly ref struct RedisScriptInterpolatedString(int literalLength, int formattedCount)
    {
        // 2 because for N holes we can have at most N + 1 literals (ignoring fragments)
        private readonly List<(string Text, Delegate? Getter)> _parts = new(capacity: (2 * formattedCount) + 1);

        public void AppendLiteral(string text) => this._parts.Add((text, null));

        public void AppendFormatted(Func<TArg, RedisKey> key, [CallerArgumentExpression(nameof(key))] string keyString = "") =>
            this._parts.Add((keyString, key));

        public void AppendFormatted(Func<TArg, RedisValue> value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
            this._parts.Add((valueString, value));

        public void AppendFormatted(Func<TArg, int> value, [CallerArgumentExpression(nameof(value))] string valueString = "") =>
            this.AppendFormatted(a => (RedisValue)value(a), valueString);

        public void AppendFormatted(RedisScriptInterpolatedString fragment) =>
            this._parts.AddRange(fragment._parts);

        public void AppendFormatted(string text, string format)
        {
            Invariant.Require(format == "r");
            this.AppendLiteral(text);
        }

        public (string, Func<TArg, RedisKey[]>, Func<TArg, RedisValue[]>) ToScript()
        {
            // 8 for KEYS[..] or ARGV[..]
            StringBuilder builder = new(capacity: literalLength + (8 * formattedCount));
            Dictionary<string, (Func<TArg, RedisKey>, int)> keys = new(capacity: formattedCount);
            Dictionary<string, (Func<TArg, RedisValue>, int)> values = new(capacity: formattedCount);

            foreach (var (text, getter) in this._parts)
            {
                if (getter is null)
                {
                    builder.Append(text);
                }
                else if (getter is Func<TArg, RedisKey> key)
                {
                    builder.Append("KEYS[").Append(GetOneBasedIndex(keys, key, text)).Append(']');
                }
                else
                {
                    builder.Append("ARGV[").Append(GetOneBasedIndex(values, (Func<TArg, RedisValue>)getter, text)).Append(']');
                }
            }

            return (
                RemoveExtraneousWhitespace(builder.ToString()),
                CreateGetter(keys),
                CreateGetter(values)
            );
        }

        private static int GetOneBasedIndex<T>(Dictionary<string, (T, int Index)> dictionary, T key, string keyString)
        {
            if (dictionary.TryGetValue(keyString, out var value)) { return value.Index + 1; }

            dictionary.Add(keyString, (key, dictionary.Count));
            return dictionary.Count; // implicitly index + 1
        }

        private static Func<TArg, T[]> CreateGetter<T>(Dictionary<string, (Func<TArg, T> Value, int Index)> dictionary) => arg =>
        {
            var result = new T[dictionary.Count];
            foreach (var pair in dictionary) { result[pair.Value.Index] = pair.Value.Value(arg); }
            return result;
        };

        // send the smallest possible script to the server
        private static string RemoveExtraneousWhitespace(string script) => Regex.Replace(script.Trim(), @"\s+", " ");
    }
}
