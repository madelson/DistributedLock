using Medallion.Threading.FileSystem;
using NUnit.Framework;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Medallion.Threading.Tests.FileSystem;

[Category("CI")]
public class FileDistributedLockTest
{
    private static readonly string LockFileDirectory = Path.Combine(Path.GetTempPath(), nameof(FileDistributedLockTest), TargetFramework.Current);
    private static DirectoryInfo LockFileDirectoryInfo => new(LockFileDirectory);

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        if (Directory.Exists(LockFileDirectory)) 
        {
            Directory.Delete(LockFileDirectory, recursive: true);
        }
    }

    [Test]
    public void TestValidatesConstructorArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new FileDistributedLock(default!));
        Assert.Throws<FormatException>(() => new FileDistributedLock(new FileInfo(LockFileDirectory + Path.DirectorySeparatorChar)));

        Assert.Throws<ArgumentNullException>(() => new FileDistributedLock(default!, "name"));
        Assert.Throws<ArgumentNullException>(() => new FileDistributedLock(LockFileDirectoryInfo, default!));
    }

    [Test, Combinatorial]
    public void TestDirectoryIsCreatedIfNeededAndFileIsCreatedIfNeededAndAlwaysDeleted(
        [Values("nothing", "directory", "file")] string alreadyExists,
        [Values] bool constructFromFileInfo)
    {
        var directoryName = Path.Combine(LockFileDirectory, Hash("directory"));
        var fileName = Path.Combine(directoryName, Hash("file"));

        switch (alreadyExists)
        {
            case "nothing":
                if (Directory.Exists(directoryName))
                {
                    Directory.Delete(directoryName, recursive: true);
                }
                break;
            case "directory":
                Directory.CreateDirectory(directoryName);
                File.Delete(fileName);
                break;
            case "file":
                Directory.CreateDirectory(directoryName);
                File.WriteAllText(fileName, "text");
                break;
            default:
                throw new InvalidOperationException("should never get here");
        }

        var @lock = constructFromFileInfo 
            ? new FileDistributedLock(new FileInfo(fileName))
            : new FileDistributedLock(new DirectoryInfo(directoryName), Path.GetFileName(fileName));
        if (constructFromFileInfo)
        {
            @lock.Name.ShouldEqual(fileName);
        }

        Directory.Exists(directoryName).ShouldEqual(alreadyExists != "nothing");
        File.Exists(fileName).ShouldEqual(alreadyExists == "file");

        using (@lock.Acquire())
        {
            Assert.IsTrue(Directory.Exists(directoryName));
            Assert.IsTrue(File.Exists(@lock.Name));
        }

        Assert.IsTrue(Directory.Exists(directoryName));
        Assert.IsFalse(File.Exists(@lock.Name));

        static string Hash(string text)
        {
            using var md5 = MD5.Create();
            var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes($"{text}_{TestContext.CurrentContext.Test.FullName}_{TargetFramework.Current}"));
            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }
    }
    
    [Test]
    public void TestFileCannotBeModifiedOrDeletedWhileHeld()
    {
        var @lock = new FileDistributedLock(LockFileDirectoryInfo, nameof(TestFileCannotBeModifiedOrDeletedWhileHeld));
        using (@lock.Acquire())
        {
            Assert.Throws<IOException>(() => File.WriteAllText(@lock.Name, "contents"), "write");
            Assert.Throws<IOException>(() => File.ReadAllText(@lock.Name), "read");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Throws<IOException>(() => File.Delete(@lock.Name), "delete");
            }
            else
            {
                // on unix, locking a file doesn't prevent unliking, so deletion effectively unlocks
                // https://stackoverflow.com/questions/2028874/what-happens-to-an-open-file-handle-on-linux-if-the-pointed-file-gets-moved-or-d
                Assert.DoesNotThrow(() => File.Delete(@lock.Name));
                using var reaquireHandle = @lock.TryAcquire();
                Assert.IsNotNull(reaquireHandle);
            }
        }
    }

    [Test]
    public void TestThrowsIfProvidedFileNameIsAlreadyADirectory()
    {
        var @lock = new FileDistributedLock(LockFileDirectoryInfo, nameof(TestThrowsIfProvidedFileNameIsAlreadyADirectory));
        Directory.CreateDirectory(@lock.Name);

        var exception = Assert.Throws<InvalidOperationException>(() => @lock.Acquire().Dispose());
        Assert.That(exception.Message, Does.Contain("because it is already the name of a directory"));
    }

    [Test]
    public void TestEmptyNameIsAllowed() => AssertCanUseName(string.Empty);

    [Test]
    public void TestLongNamesAreAllowed() => AssertCanUseName(new string('a', ushort.MaxValue));

    [Test]
    public void TestHandlesLongDirectoryNames()
    {
        DirectoryInfo tooLongDirectory;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            tooLongDirectory = AppContext.TryGetSwitch("Switch.System.IO.UseLegacyPathHandling", out var useLegacyPathHandling) && useLegacyPathHandling
                ? BuildLongDirectory(259 - (FileNameValidationHelper.MinFileNameLength - 1))
                : BuildLongDirectory(short.MaxValue - (FileNameValidationHelper.MinFileNameLength - 1));

            // we only check this on Windows currently since AppVeyor linux does not see to have a length restriction
            Assert.Throws<PathTooLongException>(() => new FileDistributedLock(tooLongDirectory, new string('a', FileNameValidationHelper.MinFileNameLength)));
        }
        else 
        {
            tooLongDirectory = BuildLongDirectory(4096 - (FileNameValidationHelper.MinFileNameLength - 1));
        }

        var almostTooLongDirectory = new DirectoryInfo(tooLongDirectory.FullName.Substring(0, tooLongDirectory.FullName.Length - 1));
        AssertCanUseName(new string('a', FileNameValidationHelper.MinFileNameLength));
        AssertCanUseName(new string('a', 100 * FileNameValidationHelper.MinFileNameLength));

        var almostTooLongBytesDirectory = new DirectoryInfo(almostTooLongDirectory.FullName.Replace("aaaa", "🦉"));
        Encoding.UTF8.GetByteCount(almostTooLongBytesDirectory.FullName).ShouldEqual(Encoding.UTF8.GetByteCount(almostTooLongDirectory.FullName));
        AssertCanUseName(new string('a', FileNameValidationHelper.MinFileNameLength));
        AssertCanUseName(new string('a', FileNameValidationHelper.MinFileNameLength + 1));

        static DirectoryInfo BuildLongDirectory(int length)
        {
            var name = new StringBuilder(LockFileDirectory);
            while (name.Length < length)
            {
                name.Append(Path.DirectorySeparatorChar)
                    .Append('a', 100); // shorter than 255 max name lengths
            }
            name.Length = length;
            // make sure we don't have separators near the end that block trimming
            name[name.Length - 1] = 'b';
            name[name.Length - 2] = 'b';
            return new DirectoryInfo(name.ToString());
        }
    }

    [TestCase(".")]
    [TestCase("..")]
    [TestCase("...")]
    [TestCase("....")]
    [TestCase("A.")]
    [TestCase("A..")]
    [TestCase(".A")]
    [TestCase("..A")]
    [TestCase(" ")]
    [TestCase("  ")]
    [TestCase("   ")]
    [TestCase("A ")]
    [TestCase(" A")]
    [TestCase(" .")]
    [TestCase(". ")]
    [TestCase(". .")]
    [TestCase(" .. ")]
    [TestCase("\t.")]
    [TestCase(" \t")]
    public void TestStrangePaths(string name) => AssertCanUseName(name);

    [TestCase("a.")]
    [TestCase("a ")]
    public void TestTrailingWhitespaceOrDotDoesNotCauseCollision(string name)
    {
        var @lock = new FileDistributedLock(LockFileDirectoryInfo, name);
        using (@lock.Acquire())
        {
            using var handle = new FileDistributedLock(LockFileDirectoryInfo, name.Trim('.').Trim(' ')).TryAcquire();
            Assert.IsNotNull(handle);
        }
    }

    [TestCase("CON")]
    [TestCase("PRN")]
    [TestCase("AUX")]
    [TestCase("NUL")]
    [TestCase("COM1")]
    [TestCase("COM2")]
    [TestCase("COM3")]
    [TestCase("COM4")]
    [TestCase("COM5")]
    [TestCase("COM6")]
    [TestCase("COM7")]
    [TestCase("COM8")]
    [TestCase("COM9")]
    [TestCase("CONIN$")]
    [TestCase("CONOUT$")]
    [TestCase("LPT1")]
    [TestCase("LPT2")]
    [TestCase("LPT3")]
    [TestCase("LPT4")]
    [TestCase("LPT5")]
    [TestCase("LPT6")]
    [TestCase("LPT7")]
    [TestCase("LPT8")]
    [TestCase("LPT9")]
    public void TestReservedWindowsNamesAreAllowed(string name)
    {
        var variants = new[]
        {
            name,
            name.ToLowerInvariant() + ".txt",
            name[0] + name.Substring(1).ToLowerInvariant(),
        };
        foreach (var variant in variants)
        {
            AssertCanUseName(variant);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                // Restrictions were alleviated in Win11: https://superuser.com/a/1742520/281669
                // Win <= 10 detection: see https://stackoverflow.com/a/69038652/1142970
                && Environment.OSVersion.Version.Build < 22000)
            {
                Assert.IsFalse(CanCreateFileWithName(variant), variant);
                Assert.AreNotEqual(name, Path.GetFileName(new FileDistributedLock(LockFileDirectoryInfo, name).Name), variant);
            }
        }
    }

    [Test]
    public void TestEscapesBadCharactersInName()
    {
        Check("A", shouldEscape: false);
        Check("A_B", shouldEscape: false);
        Check(string.Empty, shouldEscape: false);
        Check(".", shouldEscape: true);
        Check("..", shouldEscape: true);
        Check("/A/", shouldEscape: true);
        Check(@"\A\", shouldEscape: true);
        Check("<", shouldEscape: true);
        Check(">", shouldEscape: true);
        Check("\0", shouldEscape: true);

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            Check("_" + invalidChar, shouldEscape: true);
        }

        static void Check(string name, bool shouldEscape)
        {
            var @lock = new FileDistributedLock(LockFileDirectoryInfo, name);
            // ordinal required for null char comparison (see https://github.com/dotnet/runtime/issues/4673)
            @lock.Name.StartsWith(LockFileDirectory + Path.DirectorySeparatorChar + name, StringComparison.Ordinal)
                .ShouldEqual(!shouldEscape, $"'{name}', {@lock.Name}");
        }
    }

    [Test]
    public void TestForcesCaseSensitivity()
    {
        Path.GetFileName(new FileDistributedLock(LockFileDirectoryInfo, "lower").Name)
            .ShouldEqual("lowerPRE5SHQAMC324P4C6UKD4R4VMGGJMF6T.lock");
    }

    [TestCase("", ExpectedResult = "P6AD62YPPHO33YTKIBUM5WBQHQVBCSXA.lock")]
    [TestCase(".", ExpectedResult = "_LIYISOQPXAPX3NYVHPC2EK4WEOU6OW7Y.lock")]
    [TestCase("..", ExpectedResult = "__G2H2MVGLQK7QVO2MAWVKSVSKM26KX5S6.lock")]
    [TestCase("...", ExpectedResult = "___2ZDJLYOT376KUA5OQFR2OERKUXIH4A7R.lock")]
    [TestCase("LPT1", ExpectedResult = "LPT1VUGMI6NJVPIYUGXMY4K6ETA3232OR2B5.lock")]
    [TestCase(" ", ExpectedResult = "_ZPD253R4AYXN6RS7UP6A3FYDCXHBXKUY.lock")]
    [TestCase("_", ExpectedResult = "_3VP7XSELHOF3DU4D257KSSAQD3WR4SSL.lock")]
    [TestCase(@"cool<>!/:x\zzz", ExpectedResult = "cool_____x_zzzATJSHZSADXN7WTL4DBU6JULFTEDVFF3L.lock")]
    [TestCase(
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
        ExpectedResult = "aaaaaaaaaaaaaaaaaaaaaaaaaaaXTSAJQWFKOYRBNXC6DV3HRZWQJANRKCX.lock"
    )]
    [TestCase("ABC", ExpectedResult = "ABCZJ4QR6TVN4A3KMGQ4BUKHOWQYFRLXSB3.lock")]
    [TestCase("abc", ExpectedResult = "abc56LLTQOSBT6ULGHITLS4KQEIRREMO53J.lock")]
    // 255 UTF8 bytes
    [TestCase(
        "🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉123",
        ExpectedResult = "___________________________VA7XJG3JUQKTL6AU2KWPHVQFK43N4DVW.lock"
    )]
    // 256 UTF8 bytes
    [TestCase(
        "🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉🦉1234",
        ExpectedResult = "___________________________NOLT44QRLWYINT53TY2I4H34X4AU4O46.lock"
    )]
    public string TestSafeNameCompatibility(string name)
    {
        // meant to be consistent length across platforms
        var consistentDirectory = Path.Combine(LockFileDirectory, new string('b', 100)).Substring(0, 100);
        var @lock = new FileDistributedLock(new DirectoryInfo(consistentDirectory), name);
        Assert.That(@lock.Name, Does.StartWith(consistentDirectory + Path.DirectorySeparatorChar));
        return @lock.Name.Substring(consistentDirectory.Length + 1);
    }

    [Test]
    public void TestBase32Hashing()
    {
        const int Iterations = 100000;

        var charCounts = new int[128];

        var nameChars = new char[] { default, default, Path.GetInvalidFileNameChars()[0] };
        var directoryInfo = LockFileDirectoryInfo;
        for (var i = 0; i < Iterations; ++i)
        {
            if (++nameChars[0] == 0)
            {
                ++nameChars[1];
            }

            var @lock = new FileDistributedLock(directoryInfo, new string(nameChars));
            var lockFileNameWithoutExtension = Path.GetFileNameWithoutExtension(@lock.Name);
            var hashStart = lockFileNameWithoutExtension.Length - FileNameValidationHelper.HashLengthInChars;

            for (var j = hashStart; j < lockFileNameWithoutExtension.Length; ++j)
            {
                ++charCounts[lockFileNameWithoutExtension[j]];
            }
        }

        // for debugging
        //for (var i = 0; i < charCounts.Length; ++i)
        //{
        //    Console.WriteLine($"{(char.IsLetterOrDigit((char)i) ? ((char)i).ToString() : i.ToString())}: {charCounts[i]}");
        //}

        var expectedCount = (Iterations * FileNameValidationHelper.HashLengthInChars) / 32;
        for (var @char = default(char); @char < charCounts.Length; ++@char)
        {
            if ((@char >= '2' && @char <= '7') || (@char >= 'A' && @char <= 'Z'))
            {
                Assert.AreEqual(actual: charCounts[@char], expected: expectedCount, delta: .1 * expectedCount);
            }
            else
            {
                charCounts[@char].ShouldEqual(0);
            }
        }
    }

    /// <summary>
    /// Reproduces https://github.com/madelson/DistributedLock/issues/109
    /// 
    /// Basically, there is a small window where concurrent file creation/deletion throws
    /// <see cref="UnauthorizedAccessException"/> despite there being no access permission errors.
    /// See also https://github.com/dotnet/runtime/issues/61395.
    /// 
    /// This test shows that we are not vulnerable to this.
    /// </summary>
    [Test]
    public void TestDoesNotFailDueToUnauthorizedAccessExceptionOnFileCreation()
    {
        Directory.CreateDirectory(LockFileDirectory);
        var @lock = new FileDistributedLock(LockFileDirectoryInfo, Guid.NewGuid().ToString());

        const int TaskCount = 20;

        using var barrier = new Barrier(TaskCount);

        var tasks = Enumerable.Range(0, TaskCount)
            .Select(_ => Task.Factory.StartNew(() =>
            {
                barrier.SignalAndWait();

                for (var i = 0; i < 500; ++i)
                {
                    @lock.TryAcquire()?.Dispose();
                }
            }, TaskCreationOptions.LongRunning))
            .ToArray();

        Assert.DoesNotThrowAsync(() => Task.WhenAll(tasks));
    }

    /// <summary>
    /// Reproduces https://github.com/madelson/DistributedLock/issues/106
    /// 
    /// Basically, there is a small window where concurrent creation/deletion of directories
    /// throws <see cref="UnauthorizedAccessException"/> even though there are no access permission errors.
    /// 
    /// This test confirms that we recover from such errors.
    /// </summary>
    [Test]
    public void TestDoesNotFailDueToUnauthorizedAccessExceptionOnDirectoryCreation()
    {
        var @lock = new FileDistributedLock(LockFileDirectoryInfo, Guid.NewGuid().ToString());

        const int TaskCount = 20;

        using var barrier = new Barrier(TaskCount);
        using var cancelationTokenSource = new CancellationTokenSource();

        var tasks = Enumerable.Range(0, TaskCount)
            .Select(task => Task.Factory.StartNew(() =>
            {
                for (var i = 0; i < 1000; ++i)
                {
                    // line up all the threads
                    try { barrier.SignalAndWait(cancelationTokenSource.Token); }
                    catch when (cancelationTokenSource.Token.IsCancellationRequested) { return; }

                    // have one thread clear the directory
                    if (task == 0 && Directory.Exists(LockFileDirectory)) { Directory.Delete(LockFileDirectory, recursive: true); }

                    // line up all the threads
                    if (!barrier.SignalAndWait(TimeSpan.FromSeconds(3))) { throw new TimeoutException("should never get here"); }

                    // have half the threads just create and delete the directory, catching any errors
                    if (task % 2 == 0)
                    {
                        try
                        {
                            Directory.CreateDirectory(LockFileDirectory);
                            Directory.Delete(LockFileDirectory);
                        }
                        catch { }
                    }
                    // the other half will attempt to acquire the lock
                    else
                    {
                        try { @lock.TryAcquire()?.Dispose(); }
                        catch
                        {
                            cancelationTokenSource.Cancel(); // exception found: exit
                            throw;
                        }
                    }
                }
            }, TaskCreationOptions.LongRunning))
            .ToArray();

        Assert.DoesNotThrowAsync(() => Task.WhenAll(tasks));
    }

    /// <summary>
    /// Documents a limitation we've imposed for now to keep the code simpler
    /// </summary>
    [Test]
    public void TestLockingReadOnlyFileIsNotSupportedOnWindows()
    {
        // File.SetAttributes is failing on Ubuntu with FileNotFoundException even though File.Exists
        // returns true. Likely some platform compat issue with that method
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; }

        Directory.CreateDirectory(LockFileDirectory);
        var @lock = new FileDistributedLock(LockFileDirectoryInfo, Guid.NewGuid().ToString());
        File.Create(@lock.Name).Dispose();

        try
        {
            File.SetAttributes(@lock.Name, FileAttributes.ReadOnly);

            Assert.Throws<NotSupportedException>(() => @lock.TryAcquire()?.Dispose());
        }
        finally
        {
            File.SetAttributes(@lock.Name, FileAttributes.Normal);
        }
    }

    private static void AssertCanUseName(string name, DirectoryInfo? directory = null)
    {
        var @lock = new FileDistributedLock(directory ?? LockFileDirectoryInfo, name);
        IDistributedSynchronizationHandle? handle = null;
        Assert.DoesNotThrow(() => handle = @lock.TryAcquire(), name);
        Assert.IsNotNull(handle, name);
        handle!.Dispose();
    }

    private static bool CanCreateFileWithName(string name)
    {
        try
        {
            var path = Path.Combine(LockFileDirectory, name);
            File.OpenWrite(path).Dispose();
            File.Delete(path);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
