using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DistributedLockCodeGen
{
    internal static class CodeGenHelpers
    {
        public static string SolutionDirectory => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));

        public static IEnumerable<string> EnumerateSolutionFiles() => Directory.EnumerateFiles(SolutionDirectory, "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName)
            .Where(d => !Regex.IsMatch(Path.GetFileName(d), "^(DistributedLock|DistributedLock.Tests|DistributedLockCodeGen)$", RegexOptions.IgnoreCase))
            .SelectMany(d => Directory.EnumerateFiles(d, "*.cs", SearchOption.AllDirectories));

        public static string NormalizeCodeWhitespace(string code) => code.Trim().Replace("\r\n", "\n");

        public static bool HasPublicType(string code, out (string typeName, bool isInterface) info)
        {
            var match = Regex.Match(code, @"\n(    |\t)public.*?(class|interface)\s+(?<name>\w+)");
            if (match.Success)
            {
                info = (typeName: match.Groups["name"].Value, isInterface: match.Value.Contains("interface"));
                return true;
            }

            info = default;
            return false;
        }
    }
}
