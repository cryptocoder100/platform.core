#pragma warning disable CA1308 // Normalize strings to uppercase

namespace Exos.Platform.AspNetCore.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Extensions;
    using Exos.Platform.AspNetCore.Helpers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;

    /// <summary>
    /// Adds a Tracking-Id header to the request and response to aid in debugging and tracing (if not already present).
    /// </summary>
    public class TrackingIdMiddleware
    {
        private const string SAFECHARSPATTERN = @"^[a-zA-Z0-9-,]*$";
        private const string HEADERKEY = "Tracking-Id";
        private static readonly Regex SafeChars = new Regex(SAFECHARSPATTERN);
        private readonly RequestDelegate _next;
        private readonly ILogger<TrackingIdMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackingIdMiddleware"/> class.
        /// </summary>
        /// <param name="next">RequestDelegate next request.</param>
        /// <param name="logger">Logger Instance.</param>
        public TrackingIdMiddleware(RequestDelegate next, ILogger<TrackingIdMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Call the next delegate/middleware in the pipeline.
        /// </summary>
        /// <param name="context">HttpContext.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            string trackingId = context.Request.Headers[HEADERKEY];
            if (!string.IsNullOrEmpty(trackingId))
            {
                // Check for unsafe chars
                if (!SafeChars.IsMatch(trackingId))
                {
                    trackingId = context.Request.Headers[HEADERKEY] = "INVALID"; // Make safe to bubble back up to caller
                    throw new BadRequestException(HEADERKEY, $"Only the following characters may be used in a(n) {HEADERKEY} header: {SAFECHARSPATTERN}.");
                }
            }
            else
            {
                trackingId = context.Request.Headers[HEADERKEY] = Guid.NewGuid().ToString().ToLowerInvariant();
            }

            // Backfill telemetry for the current request
            TelemetryHelper.TryEnrichRequestTelemetry(context, KeyValuePair.Create("TrackingId", trackingId));

            // Include the unique ID in the response
            context.Response.OnStarting(
                state =>
                {
                    context.Response.Headers[HEADERKEY] = context.GetTrackingId();
                    context.Response.Headers["Request-Id"] = context.TraceIdentifier;
                    return Task.CompletedTask;
                }, context);

            using (_logger.BeginScope(new Dictionary<string, object>() { ["TrackingId"] = trackingId }))
            {
                await _next(context).ConfigureAwait(false);
            }
        }
    }
}
#pragma warning restore CA1308 // Normalize strings to uppercase