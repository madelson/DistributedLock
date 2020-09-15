using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.FileSystem
{
    /// <summary>
    /// A distributed lock based on holding an exclusive handle to a lock file. The file will be deleted when the lock is released.
    /// </summary>
    public sealed partial class FileDistributedLock : IInternalDistributedLock<FileDistributedLockHandle>
    {
        // These are not configurable currently because in the future we may want to change the implementation of FileDistributedLock
        // to leverage native methods which may allow for actual blocking. The values here reflect the idea that we expect file locks
        // to be used in cases where contention is rare
        private static readonly TimeSpan MinBusyWaitSleepTime = TimeSpan.FromMilliseconds(50),
            MaxBusyWaitSleepTime = TimeSpan.FromSeconds(1);

        private string? _cachedDirectory;

        /// <summary>
        /// Constructs a lock which uses the provided <paramref name="lockFile"/> as the exact file name.
        /// 
        /// Upon acquiring the lock, the file's directory will be created automatically if it does not already exist. The file 
        /// will similarly be created if it does not already exist, and will be deleted when the lock is released.
        /// </summary>
        public FileDistributedLock(FileInfo lockFile)
        {
            this.Name = (lockFile ?? throw new ArgumentNullException(nameof(lockFile))).FullName;
            if (lockFile.Name.Length == 0) { throw new FormatException($"{nameof(lockFile)}: may not have an empty file name"); }
        }

        /// <summary>
        /// Constructs a lock which will place a lock file in <paramref name="lockFileDirectory"/>. The file's name
        /// will be based on <paramref name="name"/>, but with proper escaping/hashing to ensure that a valid file name is produced.
        /// 
        /// Upon acquiring the lock, the file's directory will be created automatically if it does not already exist. The file 
        /// will similarly be created if it does not already exist, and will be deleted when the lock is released.
        /// </summary>
        public FileDistributedLock(DirectoryInfo lockFileDirectory, string name)
        {
            this.Name = FileNameValidationHelper.GetLockFileName(lockFileDirectory, name, exactName: false);
        }

        // todo revisit API
        /// <summary>
        /// Implements <see cref="IDistributedLock.Name"/>
        /// </summary>
        public string Name { get; }

        private string Directory => this._cachedDirectory ??= Path.GetDirectoryName(this.Name);

        ValueTask<FileDistributedLockHandle?> IInternalDistributedLock<FileDistributedLockHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken) =>
            BusyWaitHelper.WaitAsync(
                state: this,
                tryGetValue: (@this, token) => @this.TryAcquire(token).AsValueTask(),
                timeout: timeout,
                minSleepTime: MinBusyWaitSleepTime,
                maxSleepTime: MaxBusyWaitSleepTime,
                cancellationToken
            );

        private FileDistributedLockHandle? TryAcquire(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try { System.IO.Directory.CreateDirectory(this.Directory); }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to ensure that lock file directory {this.Directory} exists", ex);
                }

                FileStream lockFileStream;
                try
                {
                    // key arguments: 
                    // OpenOrCreate to be robust to the file existing or not
                    // None to take an exclusive lock
                    // DeleteOnClose to clean up after ourselves
                    lockFileStream = new FileStream(this.Name, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None, bufferSize: 1, FileOptions.DeleteOnClose);
                }
                catch (DirectoryNotFoundException)
                {
                    // this should almost never happen because we just created the directory but in a race condition it could. Just retry
                    continue;
                }
                catch (UnauthorizedAccessException) when (System.IO.Directory.Exists(this.Name))
                {
                    throw new InvalidOperationException($"Failed to create lock file '{this.Name}' because it is already the name of a directory");
                }
                // this should never happen because we validate. However if it does (e. g. due to some system configuration change?), throw so that
                // this doesn't end up in the IOException block (PathTooLongException is IOException)
                catch (PathTooLongException) { throw; }
                catch (IOException)
                {
                    // the hope is that if we get here the only failure reason would be that the file is locked
                    return null;
                }

                return new FileDistributedLockHandle(lockFileStream);
            }
        }
    }
}
