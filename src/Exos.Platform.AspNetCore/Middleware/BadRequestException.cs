#pragma warning disable CA1032 // Implement standard exception constructors
namespace Exos.Platform.AspNetCore.Middleware
{
    using System;
    using Microsoft.AspNetCore.Mvc.ModelBinding;

    /// <summary>
    /// Exception thrown when a request was unacceptable, often due to a missing or invalid parameter.
    /// </summary>
    public class BadRequestException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BadRequestException" /> class.
        /// </summary>
        /// <param name="modelState">The ModelState that represents the failed validation.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public BadRequestException(ModelStateDictionary modelState, Exception innerException = null) : base("One or more parameters failed validation.", innerException)
        {
            ModelState = modelState;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BadRequestException" /> class.
        /// </summary>
        /// <param name="param">The parameter that failed validation.</param>
        /// <param name="message">A user-friendly validation error message.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public BadRequestException(string param, string message, Exception innerException = null) : this(CreateModelState(param, message), innerException)
        {
        }

        /// <summary>
        /// Gets or sets the ModelStateDictionary that generated the exception.
        /// </summary>
        public ModelStateDictionary ModelState { get; set; }

        private static ModelStateDictionary CreateModelState(string param, string message)
        {
            var modelState = new ModelStateDictionary();
            modelState.TryAddModelError(param, message);

            return modelState;
        }
    }
}
#pragma warning restore CA1032 // Implement standard exception constructors