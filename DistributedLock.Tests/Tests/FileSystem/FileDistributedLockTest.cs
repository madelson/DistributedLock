using Medallion.Threading.FileSystem;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Tests.FileSystem
{
    [Category("CI")]
    public class FileDistributedLockTest
    {
        private static readonly string LockFileDirectory = Path.Combine(Path.GetTempPath(), nameof(FileDistributedLockTest), TargetFramework.Current);
        private static DirectoryInfo LockFileDirectoryInfo => new DirectoryInfo(LockFileDirectory);

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

            Assert.Throws<FormatException>(() => FileNameValidationHelper.GetLockFileName(LockFileDirectoryInfo, string.Empty, exactName: true));
            Assert.Throws<FormatException>(() => FileNameValidationHelper.GetLockFileName(LockFileDirectoryInfo, ".", exactName: true));
            Assert.Throws<FormatException>(() => FileNameValidationHelper.GetLockFileName(LockFileDirectoryInfo, "..", exactName: true));

            try { Path.GetFullPath(Path.Combine(LockFileDirectory, new string('a', 5000))); }
            catch (PathTooLongException)
            {
                Assert.Throws<FormatException>(() => FileNameValidationHelper.GetLockFileName(LockFileDirectoryInfo, new string('a', 5000), exactName: true));
            }
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
            @lock.Name.ShouldEqual(fileName);

            Directory.Exists(directoryName).ShouldEqual(alreadyExists != "nothing");
            File.Exists(fileName).ShouldEqual(alreadyExists == "file");

            using (@lock.Acquire())
            {
                Assert.IsTrue(Directory.Exists(directoryName));
                Assert.IsTrue(File.Exists(fileName));
            }

            Assert.IsTrue(Directory.Exists(directoryName));
            Assert.IsFalse(File.Exists(fileName));

            string Hash(string text)
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
                Assert.Throws<IOException>(() => File.WriteAllText(@lock.Name, "contents"));
                Assert.Throws<IOException>(() => File.ReadAllText(@lock.Name));
                Assert.Throws<IOException>(() => File.Delete(@lock.Name));
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
        public void TestEmptyNameIsAllowed()
        {
            var @lock = new FileDistributedLock(LockFileDirectoryInfo, string.Empty);
            Assert.DoesNotThrow(() => @lock.Acquire().Dispose());
        }

        [Test]
        public void TestLongNamesAreAllowed()
        {
            var @lock = new FileDistributedLock(LockFileDirectoryInfo, new string('a', ushort.MaxValue));
            Assert.DoesNotThrow(() => @lock.Acquire().Dispose());
        }

        [Test]
        public void TestEscapesBadCharactersInName()
        {
            Check("A", shouldEscape: false);
            Check("A_B", shouldEscape: false);
            Check(string.Empty, shouldEscape: true);
            Check(".", shouldEscape: true);
            Check("..", shouldEscape: true);
            Check("/A/", shouldEscape: true);
            Check(@"\A\", shouldEscape: true);
            Check("<", shouldEscape: true);
            Check(">", shouldEscape: true);

            void Check(string name, bool shouldEscape)
            {
                var @lock = new FileDistributedLock(LockFileDirectoryInfo, name);
                (@lock.Name == LockFileDirectory + Path.DirectorySeparatorChar + name)
                    .ShouldEqual(!shouldEscape);
            }
        }

        [Test]
        public void TestForcesCaseSensitivity()
        {
            Path.GetFileName(new FileDistributedLock(LockFileDirectoryInfo, "lower").Name)
                .ShouldEqual("lower_PRE5SHQAMC324P4C6UKD4R4VMGGJMF6T");
        }

        [TestCase("", ExpectedResult = "EMPTY_A4ED4E8E7DBB4757AF1BE51A3C139F84P6AD62YPPHO33YTKIBUM5WBQHQVBCSXA")]
        [TestCase(".", ExpectedResult = "_LIYISOQPXAPX3NYVHPC2EK4WEOU6OW7Y")]
        [TestCase("..", ExpectedResult = "__G2H2MVGLQK7QVO2MAWVKSVSKM26KX5S6")]
        [TestCase("_", ExpectedResult = "_")]
        [TestCase(@"cool<>!/:x\zzz", ExpectedResult = "cool__!__x_zzzATJSHZSADXN7WTL4DBU6JULFTEDVFF3L")]
        [TestCase(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            ExpectedResult = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaXTSAJQWFKOYRBNXC6DV3HRZWQJANRKCX"
        )]
        [TestCase("ABC", ExpectedResult = "ABC")]
        [TestCase("abc", ExpectedResult = "abc_56LLTQOSBT6ULGHITLS4KQEIRREMO53J")]
        public string TestSafeNameCompatibility(string name)
        {
            // meant to be consistent length across platforms
            var consistentDirectory = Path.Combine(LockFileDirectory, new string('b', 100)).Substring(0, 100);
            var @lock = new FileDistributedLock(new DirectoryInfo(consistentDirectory), name);
            Console.WriteLine(@lock.Name);
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
                var hashStart = @lock.Name.Length - FileNameValidationHelper.HashLengthInChars;
                if (@lock.Name[hashStart - 1] != '_') { Assert.Fail("Bad name format"); }

                for (var j = hashStart; j < @lock.Name.Length; ++j)
                {
                    ++charCounts[@lock.Name[j]];
                }
            }

            for (var i = 0; i < charCounts.Length; ++i)
            {
                Console.WriteLine($"{(char.IsLetterOrDigit((char)i) ? ((char)i).ToString() : i.ToString())}: {charCounts[i]}");
            }

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

        // hash char usage (should use all 32 chars about evenly across many hashes)

    }
}
