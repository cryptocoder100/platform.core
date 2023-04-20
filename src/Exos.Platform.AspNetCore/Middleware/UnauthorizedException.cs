#pragma warning disable CA1032 // Implement standard exception constructors

namespace Exos.Platform.AspNetCore.Middleware
{
    using System;

    /// <summary>
    /// Exception thrown when a request is unauthorized.
    /// </summary>
    public class UnauthorizedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
        /// </summary>
        /// <param name="message">The user-friendly exception message.</param>
        public UnauthorizedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
        /// </summary>
        /// <param name="message">The user-friendly exception message.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
#pragma warning restore CA1032 // Implement standard exception constructors