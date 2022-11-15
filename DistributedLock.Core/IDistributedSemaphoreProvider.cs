// AUTO-GENERATED
namespace Medallion.Threading;

/// <summary>
/// Acts as a factory for <see cref="IDistributedSemaphore"/> instances of a certain type. This interface may be
/// easier to use than <see cref="IDistributedSemaphore"/> in dependency injection scenarios.
/// </summary>
public interface IDistributedSemaphoreProvider
{
    /// <summary>
    /// Constructs an <see cref="IDistributedSemaphore"/> instance with the given <paramref name="name"/>.
    /// </summary>
    IDistributedSemaphore CreateSemaphore(string name, int maxCount);
}