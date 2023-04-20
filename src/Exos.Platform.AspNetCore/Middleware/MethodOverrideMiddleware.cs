namespace Exos.Platform.AspNetCore.Middleware
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Enables support for the HTTP-Method-Override header for a given request.
    /// </summary>
    public class MethodOverrideMiddleware
    {
        private const string HEADER = "Http-Method-Override";
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodOverrideMiddleware"/> class.
        /// </summary>
        /// <param name="next">Next RequestDelegate.</param>
        public MethodOverrideMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Call the next delegate/middleware in the pipeline.
        /// Catch any failure and return a formatted error response in JSON format.
        /// </summary>
        /// <param name="context">HttpContext.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Look for the override header
            IHeaderDictionary headers = context.Request.Headers;
            var overrideMethodHeader = headers[HEADER];
            if (!string.IsNullOrEmpty(overrideMethodHeader))
            {
                var overrideMethod = overrideMethodHeader.ToString();
                if ("GET".Equals(overrideMethodHeader, StringComparison.OrdinalIgnoreCase))
                {
                    // Replace the request method
                    context.Request.Method = overrideMethod.ToUpperInvariant();
                }
            }

            await _next(context).ConfigureAwait(false);
        }
    }
}
