using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DistributedLockCodeGen;

[Category("CI")]
public class DocCommentGenerator
{
    [Test]
    public void GenerateDocComments()
    {
        var changes = CodeGenHelpers.EnumerateSolutionFiles()
            .Select(f => (file: f, code: File.ReadAllText(f)))
            .Select(t => (t.file, t.code, updatedCode: AddDocComments(t.code)))
            .Where(t => CodeGenHelpers.NormalizeCodeWhitespace(t.updatedCode) != CodeGenHelpers.NormalizeCodeWhitespace(t.code))
            .ToList();
        changes.ForEach(t => File.WriteAllText(t.file, t.updatedCode));
        Assert.That(changes.Select(t => t.file), Is.Empty);
    }

    internal static string AddDocComments(string code)
    {
        if (!CodeGenHelpers.HasPublicType(code, out var typeInfo)) { return code; }

        var acquireMethods = Regex.Matches(
                code,
                $@"(?<docComment>([ \t]+///.*?\n)*)(?<indent>    (    )?|\t(\t)?){(typeInfo.isInterface ? "" : "public ")}(?<returnType>\S+) (?<name>([a-zA-Z])+)\("
                    + @"((?<paramType>\S+) (?<paramName>([a-zA-Z])+)( = \S+)(, )?)+\)"
            );

        var updatedCode = code;
        foreach (var acquireMethod in acquireMethods.Cast<Match>())
        {
            var name = acquireMethod.Groups["name"].Value;
            if (!name.Contains("Acquire")) { continue; }

            var isTry = name.StartsWith("Try");
            var isAsync = name.EndsWith("Async");
            var lockType = name.Contains("Upgradeable") ? LockType.Upgrade
                : name.Contains("Write") ? LockType.Write
                : name.Contains("Read") ? LockType.Read
                : typeInfo.typeName.Contains("Semaphore") ? LockType.Semaphore
                : LockType.Mutex;

            var @object = lockType == LockType.Semaphore ? "semaphore" : "lock";
            var handleObject = lockType == LockType.Semaphore ? "ticket" : "lock";

            var lineStart = acquireMethod.Groups["indent"].Value + "/// ";
            var docComment = new StringBuilder();

            docComment.AppendLine(lineStart + "<summary>");

            docComment.Append(lineStart);
            if (isTry) { docComment.Append("Attempts to acquire "); }
            else { docComment.Append("Acquires "); }
            docComment.Append(lockType switch
            {
                LockType.Read => "a READ lock ",
                LockType.Upgrade => "an UPGRADE lock ",
                LockType.Write => "a WRITE lock ",
                LockType.Mutex => "the lock ",
                LockType.Semaphore => "a semaphore ticket ",
                _ => throw new NotSupportedException()
            });
            docComment.Append(isAsync ? "asynchronously" : "synchronously");
            docComment.Append(isTry ? ". " : ", failing with <see cref=\"TimeoutException\"/> if the attempt times out. ");
            docComment.Append(lockType switch
            {
                LockType.Read => "Multiple readers are allowed. Not compatible with a WRITE lock. ",
                LockType.Upgrade => "Not compatible with another UPGRADE lock or a WRITE lock. ",
                LockType.Write => "Not compatible with another WRITE lock or an UPGRADE lock. ",
                LockType.Mutex => "",
                LockType.Semaphore => "",
                _ => throw new NotSupportedException(),
            });
            docComment.AppendLine("Usage: ");

            docComment.Append(lineStart).AppendLine("<code>");
            docComment.Append(lineStart).Append($"    {(isAsync ? "await " : "")}using (")
                .Append(isTry ? "var handle = " : "")
                .Append(isAsync ? "await " : "")
                .Append($"my{char.ToUpper(@object[0])}{@object[1..]}.")
                .AppendLine($"{name}(...))");
            docComment.Append(lineStart).AppendLine("    {");
            docComment.Append(lineStart).Append(' ', 8)
                .Append(isTry ? "if (handle != null) { " : "")
                .Append($"/* we have the {handleObject}! */")
                .AppendLine(isTry ? " }" : "");
            docComment.Append(lineStart).AppendLine("    }");
            docComment.Append(lineStart)
                .Append($"    // dispose releases the {handleObject}")
                .AppendLine(isTry ? " if we took it" : "");
            docComment.Append(lineStart).AppendLine("</code>");

            docComment.Append(lineStart).AppendLine("</summary>");

            docComment.Append(lineStart).Append("<param name=\"timeout\">How long to wait before giving up on the acquisition attempt. ")
                .AppendLine($"Defaults to {(isTry ? "0" : "<see cref=\"Timeout.InfiniteTimeSpan\"/>")}</param>");
            docComment.Append(lineStart).AppendLine("<param name=\"cancellationToken\">Specifies a token by which the wait can be canceled</param>");

            var returnType = acquireMethod.Groups["returnType"].Value;
            if (returnType.StartsWith("ValueTask<")) { returnType = returnType.Replace("ValueTask<", "").TrimEnd('>'); }
            var useAn = "aeiou".Contains(char.ToLower(returnType[0]));
            docComment.Append(lineStart).Append($"<returns>A{(useAn ? "n" : "")} <see cref=\"{returnType.TrimEnd('?')}\"/> which can be used to release the {handleObject}")
                .Append(isTry ? " or null on failure" : "")
                .AppendLine("</returns>");

            updatedCode = updatedCode.Replace(acquireMethod.Value, docComment + acquireMethod.Value[acquireMethod.Groups["docComment"].Length..]);
        }

        return updatedCode;
    }
}

internal enum LockType
{
    Mutex,
    Read,
    Write,
    Upgrade,
    Semaphore,
}
