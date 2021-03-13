using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DistributedLockCodeGen
{
    [Category("CI")]
    public class GenerateIDistributedLockImplementations
    {
        [Test]
        public void GenerateForIDistributedLockAndSemaphore([Values("Lock", "Semaphore")] string name)
        {
            var files = CodeGenHelpers.EnumerateSolutionFiles()
                .Where(f => !f.Contains($"Distributed{name}.Core", StringComparison.OrdinalIgnoreCase))
                .Where(f => f.EndsWith($"Distributed{name}.cs", StringComparison.OrdinalIgnoreCase) && Path.GetFileName(f)[0] != 'I');
            
            var errors = new List<string>();
            foreach (var file in files)
            {
                var lockCode = File.ReadAllText(file);
                if (lockCode.Contains("AUTO-GENERATED")
                    || !CodeGenHelpers.HasPublicType(lockCode, out _)) 
                { 
                    continue; 
                }

                if (!lockCode.Contains($": IInternalDistributed{name}<"))
                {
                    errors.Add($"{file} does not implement the expected interface");
                    continue;
                }

                var lockType = Path.GetFileNameWithoutExtension(file);
                var handleType = lockType + "Handle";
                // zookeeper is inherently asynchronous (watch-based), so any synchronous APIs it has are just sync-over-async
                var includeSynchronousApis = !file.Contains("ZooKeeper", StringComparison.OrdinalIgnoreCase);

                var explicitImplementations = new StringBuilder();
                var @interface = $"IDistributed{name}";
                foreach (var method in new[] { "TryAcquire", "Acquire", "TryAcquireAsync", "AcquireAsync" })
                {
                    AppendExplicitInterfaceMethod(
                        explicitImplementations, 
                        @interface, 
                        method, 
                        "IDistributedSynchronizationHandle",
                        @as: includeSynchronousApis ? null : $"IInternalDistributed{name}<{handleType}>"
                    );
                }

                var @namespace = Regex.Match(lockCode, @"\nnamespace (?<namespace>\S+)").Groups["namespace"].Value;
                var code =
$@"using System;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading.Internal;

namespace {@namespace}
{{
    public partial class {lockType}
    {{
        // AUTO-GENERATED

{explicitImplementations}
        {IfSyncApis("public ")}{handleType}? {(includeSynchronousApis ? "" : $"IInternalDistributed{name}<{handleType}>.")}TryAcquire(TimeSpan timeout{IfSyncApis(" = default")}, CancellationToken cancellationToken{IfSyncApis(" = default")}) =>
            DistributedLockHelpers.TryAcquire(this, timeout, cancellationToken);

        {IfSyncApis("public ")}{handleType} {(includeSynchronousApis ? "" : $"IInternalDistributed{name}<{handleType}>.")}Acquire(TimeSpan? timeout{IfSyncApis(" = null")}, CancellationToken cancellationToken{IfSyncApis(" = default")}) =>
            DistributedLockHelpers.Acquire(this, timeout, cancellationToken);

        public ValueTask<{handleType}?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            this.As<IInternalDistributed{name}<{handleType}>>().InternalTryAcquireAsync(timeout, cancellationToken);

        public ValueTask<{handleType}> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.AcquireAsync(this, timeout, cancellationToken);
    }}
}}";
                code = DocCommentGenerator.AddDocComments(code);

                var outputPath = Path.Combine(Path.GetDirectoryName(file)!, Path.GetFileNameWithoutExtension(file) + $".IDistributed{name}.cs");
                if (!File.Exists(outputPath) || CodeGenHelpers.NormalizeCodeWhitespace(File.ReadAllText(outputPath)) != CodeGenHelpers.NormalizeCodeWhitespace(code))
                {
                    File.WriteAllText(outputPath, code);
                    errors.Add($"updated {file}");
                }

                string IfSyncApis(string value) => includeSynchronousApis ? value : string.Empty;
            }

            Assert.IsEmpty(errors);
        }

        [Test]
        public void GenerateForIDistributedReaderWriterLock()
        {
            var files = CodeGenHelpers.EnumerateSolutionFiles()
                .Where(f => f.IndexOf("DistributedLock.Core", StringComparison.OrdinalIgnoreCase) < 0)
                .Where(f => Regex.IsMatch(Path.GetFileName(f), @"Distributed.*?ReaderWriterLock\.cs$", RegexOptions.IgnoreCase));

            var errors = new List<string>();
            foreach (var file in files)
            {
                var lockCode = File.ReadAllText(file);
                if (lockCode.Contains("AUTO-GENERATED")
                    || !CodeGenHelpers.HasPublicType(lockCode, out _)) 
                { 
                    continue; 
                }

                bool isUpgradeable;
                if (lockCode.Contains(": IInternalDistributedUpgradeableReaderWriterLock<"))
                {
                    isUpgradeable = true;
                }
                else if (lockCode.Contains(": IInternalDistributedReaderWriterLock<"))
                {
                    isUpgradeable = false;
                }
                else
                {
                    errors.Add($"{file} does not implement the expected interface");
                    continue;
                }

                var lockType = Path.GetFileNameWithoutExtension(file);

                var explicitImplementations = new StringBuilder();
                var publicMethods = new StringBuilder();
                foreach (var methodLockType in new[] { LockType.Read, LockType.Upgrade, LockType.Write }.Where(t => isUpgradeable || t != LockType.Upgrade))
                foreach (var isAsync in new[] { false, true })
                foreach (var isTry in new[] { true, false })
                {
                    var upgradeableText = methodLockType == LockType.Upgrade ? "Upgradeable" : "";
                    var handleType = lockType + upgradeableText + "Handle";

                    var methodName = $"{(isTry ? "Try" : "")}Acquire{upgradeableText}{(methodLockType == LockType.Write ? "Write" : "Read")}Lock{(isAsync ? "Async" : "")}";
                    AppendExplicitInterfaceMethod(
                        explicitImplementations,
                        $"IDistributed{upgradeableText}ReaderWriterLock",
                        methodName, 
                        $"IDistributed{(methodLockType == LockType.Upgrade ? "LockUpgradeable" : "Synchronization")}Handle"
                    );

                    var simplifiedMethodName = methodLockType == LockType.Upgrade ? methodName : methodName.Replace("ReadLock", "").Replace("WriteLock", "");

                    publicMethods.AppendLine()
                        .Append(' ', 8).Append("public ")
                        .Append(isAsync ? "ValueTask<" : "").Append(handleType).Append(isTry ? "?" : "").Append(isAsync ? ">" : "").Append(' ')
                        .Append(methodName)
                        .Append("(").Append("TimeSpan").Append(isTry ? "" : "?").AppendLine($" timeout = {(isTry ? "default" : "null")}, CancellationToken cancellationToken = default) =>")
                        .Append(' ', 12)
                        .Append(
                            isTry && isAsync
                                ? $"this.As<IInternalDistributed{upgradeableText}ReaderWriterLock<{(methodLockType == LockType.Upgrade ? lockType + "Handle, " : "")}{handleType}>>()"
                                    + $".Internal{simplifiedMethodName}(timeout, cancellationToken"
                                : $"DistributedLockHelpers.{simplifiedMethodName}(this, timeout, cancellationToken"
                        )
                        .Append(methodLockType == LockType.Read ? ", isWrite: false" : methodLockType == LockType.Write ? ", isWrite: true" : "")
                        .AppendLine(");");
                }

                var @namespace = Regex.Match(lockCode, @"\nnamespace (?<namespace>\S+)").Groups["namespace"].Value;
                var code =
$@"using System;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading.Internal;

namespace {@namespace}
{{
    public partial class {lockType}
    {{
        // AUTO-GENERATED

{explicitImplementations}{publicMethods}
    }}
}}";
                code = DocCommentGenerator.AddDocComments(code);

                var outputPath = Path.Combine(Path.GetDirectoryName(file)!, $"{Path.GetFileNameWithoutExtension(file)}.IDistributed{(isUpgradeable ? "Upgradeable" : "")}ReaderWriterLock.cs");
                if (!File.Exists(outputPath) || CodeGenHelpers.NormalizeCodeWhitespace(File.ReadAllText(outputPath)) != CodeGenHelpers.NormalizeCodeWhitespace(code))
                {
                    File.WriteAllText(outputPath, code);
                    errors.Add($"updated {file}");
                }
            }

            Assert.IsEmpty(errors);
        }

        private static void AppendExplicitInterfaceMethod(StringBuilder code, string @interface, string method, string returnType, string? @as = null)
        {
            var isAsync = method.EndsWith("Async");
            var isTry = method.StartsWith("Try");
            var returnTypeToUse = isTry ? returnType + "?" : returnType;

            code.Append(' ', 8)
                .Append(isAsync ? $"ValueTask<{returnTypeToUse}>" : returnTypeToUse)
                .AppendLine($" {@interface}.{method}(TimeSpan{(isTry ? string.Empty : "?")} timeout, CancellationToken cancellationToken) =>")
                .Append(' ', 12)
                .Append($"this{(@as != null ? $".As<{@as}>()" : "")}.{method}(timeout, cancellationToken)")
                .Append(isAsync ? $".Convert(To<{returnTypeToUse}>.ValueTask)" : string.Empty)
                .AppendLine(";");
        }
    }
}
