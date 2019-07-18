using System;

namespace Entia
{
    /// <summary>
    /// Tag interface that all messages must implement.
    /// </summary>
    public interface IMessage { }

    namespace Messages
    {
        /// <summary>
        /// Message emitted after an exception is thrown.
        /// </summary>
        /// <seealso cref="IMessage" />
        public struct OnException : IMessage
        {
            /// <summary>
            /// The thrown exception.
            /// </summary>
            public Exception Exception;
        }
    }
}
