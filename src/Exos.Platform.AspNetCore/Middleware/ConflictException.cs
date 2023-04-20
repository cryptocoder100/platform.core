namespace Exos.Platform.AspNetCore.Middleware
{
    using System;

    /// <summary>
    /// Exception thrown when a request could not be completed due to a concurrency conflict.
    /// </summary>
    public class ConflictException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class.
        /// </summary>
        /// <param name="message">The user-friendly exception message.</param>
        public ConflictException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class.
        /// </summary>
        /// <param name="message">The user-friendly exception message.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        public ConflictException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class.
        /// </summary>
        public ConflictException()
        {
        }
    }
}
