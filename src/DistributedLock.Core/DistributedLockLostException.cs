namespace Medallion.Threading;

/// <summary>
/// An exception thrown when acquiring multiple locks/primitives fails because an already-acquired
/// lock was lost before all of them could be acquired
/// </summary>
[Serializable]
public sealed class DistributedLockLostException : InvalidOperationException
{
    /// <summary>
    /// Constructs a new instance of <see cref="DistributedLockLostException"/> with a default message
    /// </summary>
    public DistributedLockLostException() : this("A lock was lost during acquisition") { }

    /// <summary>
    /// Constructs an instance of <see cref="DistributedLockLostException"/> with the given <paramref name="message"/>
    /// </summary>
    public DistributedLockLostException(string message) : base(message) { }

    /// <summary>
    /// Constructs an instance of <see cref="DistributedLockLostException"/> with the given <paramref name="message"/> and <paramref name="innerException"/>
    /// </summary>
    public DistributedLockLostException(string message, Exception innerException) : base(message, innerException) { }

#if NET8_0_OR_GREATER
    [Obsolete] // calls obsolete constructor
#endif
    private DistributedLockLostException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
