#pragma warning disable CA1032 // Implement standard exception constructors

namespace Exos.Platform.AspNetCore.Middleware
{
    using System;

    /// <summary>
    /// Exception thrown when the requested resource doesn't exist.
    /// </summary>
    public class NotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class.
        /// </summary>
        /// <param name="param">The name of the missing object's identifier parameter, e.g. 'Id'.</param>
        /// <param name="message">A user-friendly message explaining the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public NotFoundException(string param, string message, Exception innerException = null) : base(message, innerException)
        {
            Param = param;
        }

        /// <summary>
        /// Gets or sets the name of the missing object's identifier parameter, e.g. 'Id'..
        /// </summary>
        public string Param { get; set; }
    }
}
#pragma warning restore CA1032 // Implement standard exception constructors