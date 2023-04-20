#pragma warning disable CA1032 // Implement standard exception constructors

namespace Exos.Platform.AspNetCore.Middleware
{
    using System;
    using Exos.Platform.AspNetCore.Models;

    /// <summary>
    /// Thrown when an error model is received from a service response.
    /// This is most often used by a gateway to re-throw an exception from a proxied API.
    /// </summary>
    public class ErrorModelException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorModelException"/> class.
        /// </summary>
        /// <param name="model">The serialized error returned from a proxied API.</param>
        public ErrorModelException(ErrorModel model) : base(model != null ? model.Message : "Error Model Exception.")
        {
            Model = model;
        }

        /// <summary>
        /// Gets the <see cref="ErrorModel" /> representing the original error..
        /// </summary>
        public ErrorModel Model { get; private set; }
    }
}
#pragma warning restore CA1032 // Implement standard exception constructors