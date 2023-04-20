namespace Exos.Platform.AspNetCore.Middleware
{
    using System;

    /// <summary>
    /// Configuration options for the <see cref="ErrorHandlerMiddleware" />.
    /// </summary>
    public class ErrorHandlerMiddlewareOptions
    {
        /// <summary>
        /// Gets or sets the ExceptionFilter
        /// Gets or sets a callback for inspecting or transforming a global exception prior to being handled by the <see cref="ErrorHandlerMiddleware" />..
        /// </summary>
        public Func<Exception, Exception> ExceptionFilter { get; set; }
    }
}