namespace Exos.Platform.AspNetCore.Middleware
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// ValidateAntiforgeryTokenMiddleware, this validates the CSRF header.
    /// </summary>
    public class ValidateAntiforgeryTokenMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ValidateAntiforgeryTokenMiddleware> _logger;
        private readonly IAntiforgeryService _antiforgeryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateAntiforgeryTokenMiddleware"/> class.
        /// </summary>
        /// <param name="next">RequestDelegate.</param>
        /// <param name="logger">ILogger.</param>
        /// <param name="antiforgeryService">IAntiforgeryService.</param>
        public ValidateAntiforgeryTokenMiddleware(RequestDelegate next, ILogger<ValidateAntiforgeryTokenMiddleware> logger, IAntiforgeryService antiforgeryService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _antiforgeryService = antiforgeryService ?? throw new ArgumentNullException(nameof(antiforgeryService));
        }

        /// <summary>
        /// This enforces/validates the CSRF headers.
        /// </summary>
        /// <param name="context">HttpContext.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task Invoke(HttpContext context)
        {
            if (context != null)
            {
                var pathAndQuery = $"{context.Request.Path}{context.Request.QueryString}";
                try
                {
                    _logger.LogTrace("ValidateAntiforgeryTokenMiddleware invokes for '{PathAndQuery}'.", pathAndQuery);
                    _antiforgeryService.ValidateRequestAsync(context);
                    _logger.LogTrace("ValidateAntiforgeryTokenMiddleware validated for '{PathAndQuery}'.", pathAndQuery);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Anti-forgery validation failed, no csrf Or expired/invalid token provided in request header for '{PathAndQuery}'.", pathAndQuery);
                    throw new UnauthorizedException($"Anti-forgery validation failed, no csrf Or expired/invalid token provided in request header for '{pathAndQuery}'.", e);
                }
            }

            await _next.Invoke(context).ConfigureAwait(false);
        }
    }
}
