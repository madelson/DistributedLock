using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DistributedLockCodeGen
{
    public class GenerateIDistributedLockImplementations
    {
        public static string SolutionDirectory => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));

        [Test]
        public void Generate()
        {
            var files = Directory.GetFiles(SolutionDirectory, "*DistributedLock.cs", SearchOption.AllDirectories)
                .Where(f => f.IndexOf("DistributedLock.Tests", StringComparison.OrdinalIgnoreCase) < 0
                    && f.IndexOf("DistributedLock.Core", StringComparison.OrdinalIgnoreCase) < 0);
            
            var errors = new List<string>();
            foreach (var file in files)
            {
                var lockCode = File.ReadAllText(file);
                if (!lockCode.Contains(": IInternalDistributedLock<"))
                {
                    errors.Add($"{file} does not implement the expected interface");
                    continue;
                }

                var lockType = Path.GetFileNameWithoutExtension(file);
                var handleType = lockType + "Handle";
                var handleTypeArticle = new[] { "A", "E", "I", "O", "U" }.Any(s => handleType.StartsWith(s)) ? "An" : "A";

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

        IDistributedLockHandle? IDistributedLock.TryAcquire(TimeSpan timeout, CancellationToken cancellationToken) =>
            this.TryAcquire(timeout, cancellationToken);

        IDistributedLockHandle IDistributedLock.Acquire(TimeSpan? timeout, CancellationToken cancellationToken) =>
            this.Acquire(timeout, cancellationToken);

        ValueTask<IDistributedLockHandle?> IDistributedLock.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            Helpers.ConvertValueTask<{handleType}?, IDistributedLockHandle?>(this.TryAcquireAsync(timeout, cancellationToken));

        ValueTask<IDistributedLockHandle> IDistributedLock.AcquireAsync(TimeSpan? timeout, CancellationToken cancellationToken) =>
            Helpers.ConvertValueTask<{handleType}, IDistributedLockHandle>(this.AcquireAsync(timeout, cancellationToken));

        /// <summary>
        /// Attempts to acquire the lock synchronously. Usage:
        /// <code>
        ///     using (var handle = myLock.TryAcquire(...))
        ///     {{
        ///         if (handle != null) {{ /* we have the lock! */ }}
        ///     }}
        ///     // dispose releases the lock if we took it
        /// </code>
        /// </summary>
        /// <param name=""timeout"">How long to wait before giving up on acquiring the lock. Defaults to 0</param>
        /// <param name=""cancellationToken"">Specifies a token by which the wait can be canceled</param>
        /// <returns>{handleTypeArticle} <see cref=""{handleType}""/> which can be used to release the lock, or null if the lock was not taken</returns>
        public {handleType}? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.TryAcquire(this, timeout, cancellationToken);

        /// <summary>
        /// Acquires the lock synchronously, failing with <see cref=""TimeoutException""/> if the wait times out
        /// <code>
        ///     using (myLock.Acquire(...))
        ///     {{
        ///         // we have the lock
        ///     }}
        ///     // dispose releases the lock
        /// </code>
        /// </summary>
        /// <param name=""timeout"">How long to wait before giving up on acquiring the lock. Defaults to <see cref=""Timeout.InfiniteTimeSpan""/></param>
        /// <param name=""cancellationToken"">Specifies a token by which the wait can be canceled</param>
        /// <returns>{handleTypeArticle} <see cref=""{handleType}""/> which can be used to release the lock</returns>
        public {handleType} Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.Acquire(this, timeout, cancellationToken);

        /// <summary>
        /// Attempts to acquire the lock asynchronously. Usage:
        /// <code>
        ///     using (var handle = await myLock.TryAcquireAsync(...))
        ///     {{
        ///         if (handle != null) {{ /* we have the lock! */ }}
        ///     }}
        ///     // dispose releases the lock if we took it
        /// </code>
        /// </summary>
        /// <param name=""timeout"">How long to wait before giving up on acquiring the lock. Defaults to 0</param>
        /// <param name=""cancellationToken"">Specifies a token by which the wait can be canceled</param>
        /// <returns>{handleTypeArticle} <see cref=""{handleType}""/> which can be used to release the lock, or null if the lock was not taken</returns>
        public ValueTask<{handleType}?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            this.As<IInternalDistributedLock<{handleType}>>().InternalTryAcquireAsync(timeout, cancellationToken);

        /// <summary>
        /// Acquires the lock asynchronously, failing with <see cref=""TimeoutException""/> if the wait times out
        /// <code>
        ///     using (await myLock.AcquireAsync(...))
        ///     {{
        ///         // we have the lock
        ///     }}
        ///     // dispose releases the lock
        /// </code>
        /// </summary>
        /// <param name=""timeout"">How long to wait before giving up on acquiring the lock. Defaults to <see cref=""Timeout.InfiniteTimeSpan""/></param>
        /// <param name=""cancellationToken"">Specifies a token by which the wait can be canceled</param>
        /// <returns>{handleTypeArticle} <see cref=""{handleType}""/> which can be used to release the lock</returns>
        public ValueTask<{handleType}> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.AcquireAsync(this, timeout, cancellationToken);
    }}
}}";
                var outputPath = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".IDistributedLock.cs");
                if (!File.Exists(outputPath) || File.ReadAllText(outputPath) != code)
                {
                    File.WriteAllText(outputPath, code);
                    errors.Add($"updated {file}");
                }
            }

            Assert.IsEmpty(errors);
        }
    }
}
