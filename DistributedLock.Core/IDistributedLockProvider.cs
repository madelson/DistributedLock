// AUTO-GENERATED
namespace Medallion.Threading
{
    /// <summary>
    /// Acts as a factory for <see cref="IDistributedLock"/> instances of a certain type. This interface may be
    /// easier to use than <see cref="IDistributedLock"/> in dependency injection scenarios.
    /// </summary>
    public interface IDistributedLockProvider
    {
        /// <summary>
        /// Constructs an <see cref="IDistributedLock"/> instance with the given <paramref name="name"/>.
        /// </summary>
        IDistributedLock CreateLock(string name);
    }
}