namespace Exos.Platform.AspNetCore.Middleware
{
    using System;
    using Microsoft.AspNetCore.Builder;

    /// <summary>
    /// Provides extensions for registering EXOS exception handling middleware.
    /// </summary>
    public static class ErrorHandlerMiddlewareExtensions
    {
        /// <summary>
        /// Adds a middleware to the pipeline that will return a properly formatted JSON error when an exception occurs.
        /// </summary>
        /// <param name="app">An instance of <see cref="IApplicationBuilder" />.</param>
        /// <returns>An configured instance of <see cref="IApplicationBuilder"/> with the ErrorHandlerMiddleware.</returns>
        public static IApplicationBuilder UseErrorHandler(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<ErrorHandlerMiddleware>();
        }

        /// <summary>
        /// Adds a middleware to the pipeline that will return a properly formatted JSON error when an exception occurs.
        /// </summary>
        /// <param name="app">An instance of <see cref="IApplicationBuilder" />.</param>
        /// <param name="setupAction">A callback for configuring <see cref="ErrorHandlerMiddlewareOptions" />.</param>
        /// <returns>An configured instance of <see cref="IApplicationBuilder"/> with the ErrorHandlerMiddleware.</returns>
        public static IApplicationBuilder UseErrorHandler(this IApplicationBuilder app, Action<ErrorHandlerMiddlewareOptions> setupAction)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            var options = new ErrorHandlerMiddlewareOptions();
            setupAction(options);

            return app.UseMiddleware<ErrorHandlerMiddleware>(options);
        }
    }
}
