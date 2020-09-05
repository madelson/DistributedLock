using Medallion.Threading.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Tests.FileSystem
{
    [Category("CI")]
    public class FileLockTest
    {
        [Test]
        public void Test()
        {
            var lockFile = Path.GetTempFileName();
            var @lock = new FileLock(lockFile);

            using var handle1 = @lock.Acquire(TimeSpan.FromSeconds(1));
            var task = Task.Run(() => @lock.Acquire(TimeSpan.FromSeconds(30)));
            task.Wait(TimeSpan.FromSeconds(.5)).ShouldEqual(false);
            handle1.Dispose();
            task.Wait(TimeSpan.FromSeconds(10)).ShouldEqual(true);
            task.Result.Dispose();
        }

        private class FileLock
        {
            private readonly string _path;

            public FileLock(string path)
            {
                this._path = path;
            }

            public IDisposable Acquire(TimeSpan timeout)
            {
                var result = SyncOverAsync.Run(
                    state => Threading.Azure.BusyWaitHelper.WaitAsync<string, FileStream>(
                        this._path,
                        (path, token) =>
                        {
                            token.ThrowIfCancellationRequested();
                            try { return new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None, 1, FileOptions.DeleteOnClose).As<FileStream?>().AsValueTask(); }
                            catch (IOException) { return default; }
                        },
                        timeout,
                        TimeSpan.FromSeconds(.1),
                        TimeSpan.FromSeconds(2),
                        default
                    ),
                    (timeout, this._path)
                );
                if (result == null) { throw new TimeoutException(); }
                return result;
            }
        }
    }
}
