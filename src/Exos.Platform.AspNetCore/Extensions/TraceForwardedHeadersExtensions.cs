namespace Exos.Platform.AspNetCore.Extensions
{
    using System;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Encryption;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Defines the <see cref="TraceForwardedHeadersExtensions" />.
    /// </summary>
    public static class TraceForwardedHeadersExtensions
    {
        /// <summary>
        /// Trace the original X-Forwarded headers to the request telemetry before they are processed.
        /// </summary>
        /// <param name="applicationBuilder">The applicationBuilder<see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> configured to forward http headers.</returns>
        public static IApplicationBuilder TraceForwardedHeaders(this IApplicationBuilder applicationBuilder)
        {
            if (applicationBuilder == null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            applicationBuilder.Use(TraceForwardedHeaders);

            return applicationBuilder;
        }

        private static Task TraceForwardedHeaders(HttpContext context, Func<Task> next)
        {
            RequestTelemetry requestTelemetry = context.Features.Get<RequestTelemetry>();
            if (requestTelemetry != null)
            {
                if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
                {
                    requestTelemetry.Properties["X-Forwarded-For"] = forwardedFor.ToString();
                }

                if (context.Request.Headers.TryGetValue("X-Forwarded-Host", out var forwardedHost))
                {
                    requestTelemetry.Properties["X-Forwarded-Host"] = forwardedHost.ToString();
                }

                if (context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var forwardedProto))
                {
                    requestTelemetry.Properties["X-Forwarded-Proto"] = forwardedProto.ToString();
                }

                if (context.Request.Headers.TryGetValue(EncryptionConstants.EncryptionRequestHeader, out var encryptionRequestHeader))
                {
                    requestTelemetry.Properties[EncryptionConstants.EncryptionRequestHeader] = encryptionRequestHeader.ToString();
                }
            }

            return next.Invoke();
        }
    }
}
