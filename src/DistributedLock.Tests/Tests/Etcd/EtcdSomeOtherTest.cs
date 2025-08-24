using Medallion.Threading.FileSystem;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Etcd;

public class EtcdDistributedLockWindowsTest
{
    /// <summary>
    /// Example of where always ignoring <see cref="UnauthorizedAccessException"/> during file creation
    /// would be problematic.
    /// </summary>
    [Test]
    public void TestThrowsUnauthorizedAccessExceptionInCaseOfFilePermissionViolation()
    {
        var @lock = new FileDistributedLock(new DirectoryInfo(@"C:\Windows"), Guid.NewGuid().ToString());
        Assert.Throws<UnauthorizedAccessException>(() => @lock.TryAcquire()?.Dispose());
    }

    /// <summary>
    /// Example of where always ignoring <see cref="UnauthorizedAccessException"/> during directory creation
    /// would be problematic.
    /// </summary>
    [Test]
    public void TestThrowsUnauthorizedAccessExceptionInCaseOfDirectoryPermissionViolation()
    {
        var @lock = new FileDistributedLock(new DirectoryInfo(@"C:\Windows\MedallionDistributedLock"), Guid.NewGuid().ToString());
        var exception = Assert.Throws<InvalidOperationException>(() => @lock.TryAcquire()?.Dispose())!;
        Assert.That(exception.InnerException, Is.InstanceOf<UnauthorizedAccessException>());
        Assert.That(Directory.Exists(Path.GetDirectoryName(@lock.Name)), Is.False);
    }
}
