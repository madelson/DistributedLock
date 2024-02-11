using Medallion.Threading.Internal;

namespace Medallion.Threading.FileSystem;

/// <summary>
/// Implements <see cref="IDistributedSynchronizationHandle"/>
/// </summary>
public sealed class FileDistributedLockHandle : IDistributedSynchronizationHandle
{
    private FileStream? _fileStream;

    internal FileDistributedLockHandle(FileStream fileStream)
    {
        this._fileStream = fileStream;
    }

    CancellationToken IDistributedSynchronizationHandle.HandleLostToken => 
        Volatile.Read(ref this._fileStream) != null ? CancellationToken.None : throw this.ObjectDisposed();

    /// <summary>
    /// Releases the lock
    /// </summary>
    public void Dispose() => Interlocked.Exchange(ref this._fileStream, null)?.Dispose();

    /// <summary>
    /// Releases the lock
    /// </summary>
    public ValueTask DisposeAsync()
    {
        this.Dispose();
        return default;
    }
}
