namespace Medallion.Threading.FileSystem;

/// <summary>
/// Implements <see cref="IDistributedLockProvider"/> for <see cref="FileDistributedLock"/>
/// </summary>
public sealed class FileDistributedSynchronizationProvider : IDistributedLockProvider
{
    private readonly DirectoryInfo _lockFileDirectory;

    /// <summary>
    /// Constructs a provider that scopes lock files within the provided <paramref name="lockFileDirectory"/>.
    /// </summary>
    public FileDistributedSynchronizationProvider(DirectoryInfo lockFileDirectory)
    {
        this._lockFileDirectory = lockFileDirectory ?? throw new ArgumentNullException(nameof(lockFileDirectory));
    }

    /// <summary>
    /// Constructs a <see cref="FileDistributedLock"/> with the given <paramref name="name"/>.
    /// </summary>
    public FileDistributedLock CreateLock(string name) => new(this._lockFileDirectory, name);

    IDistributedLock IDistributedLockProvider.CreateLock(string name) => this.CreateLock(name);
}
