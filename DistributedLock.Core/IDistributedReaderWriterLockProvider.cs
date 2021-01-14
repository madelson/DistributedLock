// AUTO-GENERATED
namespace Medallion.Threading
{
    /// <summary>
    /// Acts as a factory for <see cref="IDistributedReaderWriterLock"/> instances of a certain type. This interface may be
    /// easier to use than <see cref="IDistributedReaderWriterLock"/> in dependency injection scenarios.
    /// </summary>
    public interface IDistributedReaderWriterLockProvider
    {
        /// <summary>
        /// Constructs an <see cref="IDistributedReaderWriterLock"/> instance with the given <paramref name="name"/>.
        /// </summary>
        IDistributedReaderWriterLock CreateReaderWriterLock(string name);
    }
}