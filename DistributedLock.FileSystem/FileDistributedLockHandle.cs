using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.FileSystem
{
    public sealed class FileDistributedLockHandle : IDistributedLockHandle
    {
        private FileStream? _fileStream;

        internal FileDistributedLockHandle(FileStream fileStream)
        {
            this._fileStream = fileStream;
        }

        CancellationToken IDistributedLockHandle.HandleLostToken => 
            Volatile.Read(ref this._fileStream) != null ? CancellationToken.None : throw this.ObjectDisposed();

        public void Dispose() => Interlocked.Exchange(ref this._fileStream, null)?.Dispose();

        public ValueTask DisposeAsync()
        {
            this.Dispose();
            return default;
        }
    }
}
