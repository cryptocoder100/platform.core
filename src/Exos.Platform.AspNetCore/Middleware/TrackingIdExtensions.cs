using System;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Exos.Platform.AspNetCore.Middleware
{
    /// <summary>
    /// Provides extensions for registering TrackingIdMiddleware.
    /// </summary>
    public static class TrackingIdExtensions
    {
        /// <summary>
        /// Adds the <see cref="TrackingIdMiddleware" /> that will include a unique GUID tracking ID value to every request and response.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder" /> to configure.</param>
        /// <returns>The configured <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseTrackingId(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseMiddleware<TrackingIdMiddleware>();
        }
    }
}
