// AUTO-GENERATED
namespace Medallion.Threading
{
    /// <summary>
    /// Acts as a factory for <see cref="IDistributedUpgradeableReaderWriterLock"/> instances of a certain type. This interface may be
    /// easier to use than <see cref="IDistributedUpgradeableReaderWriterLock"/> in dependency injection scenarios.
    /// </summary>
    public interface IDistributedUpgradeableReaderWriterLockProvider: IDistributedReaderWriterLockProvider
    {
        /// <summary>
        /// Constructs an <see cref="IDistributedUpgradeableReaderWriterLock"/> instance with the given <paramref name="name"/>.
        /// </summary>
        IDistributedUpgradeableReaderWriterLock CreateUpgradeableReaderWriterLock(string name);
    }
}