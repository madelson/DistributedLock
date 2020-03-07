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
    public sealed class DeadlockExceptionOld 
        // for backwards compat
        : InvalidOperationException
    {
        /// <summary>
        /// Constructs a new instance of <see cref="DeadlockExceptionOld"/> with a default message
        /// </summary>
        public DeadlockExceptionOld() : this("A deadlock occurred") { }

        /// <summary>
        /// Constructs an instance of <see cref="DeadlockExceptionOld"/> with the given <paramref name="message"/>
        /// </summary>
        public DeadlockExceptionOld(string message) : base(message) { }

        /// <summary>
        /// Constructs an instance of <see cref="DeadlockExceptionOld"/> with the given <paramref name="message"/> and <paramref name="innerException"/>
        /// </summary>
        public DeadlockExceptionOld(string message, Exception innerException) : base(message, innerException) { }

#if !NETSTANDARD1_3
        private DeadlockExceptionOld(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}
