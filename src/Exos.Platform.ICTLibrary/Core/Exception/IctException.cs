namespace Exos.Platform.ICTLibrary.Core.Exception
{
    using System;

    /// <summary>
    /// To handle errors for ICT library.
    /// </summary>
    public class IctException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IctException"/> class.
        /// </summary>
        public IctException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IctException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public IctException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IctException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner"> The exception that is the cause of the current exception.</param>
        public IctException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}