using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading
{
    /// <summary>
    /// An exception that SOME distributed locks will throw under SOME deadlock conditions. Note that even locks
    /// that throw this exception under some circumstances cannot detect ALL deadlock conditions
    /// </summary>
#if !NETSTANDARD1_3
    [Serializable]
#endif
    public sealed class DeadlockException 
        // for backwards compat
        : InvalidOperationException
    {
        /// <summary>
        /// Constructs a new instance of <see cref="DeadlockException"/> with a default message
        /// </summary>
        public DeadlockException() : this("A deadlock occurred") { }

        /// <summary>
        /// Constructs an instance of <see cref="DeadlockException"/> with the given <paramref name="message"/>
        /// </summary>
        public DeadlockException(string message) : base(message) { }

        /// <summary>
        /// Constructs an instance of <see cref="DeadlockException"/> with the given <paramref name="message"/> and <paramref name="innerException"/>
        /// </summary>
        public DeadlockException(string message, Exception innerException) : base(message, innerException) { }

#if !NETSTANDARD1_3
        private DeadlockException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}
