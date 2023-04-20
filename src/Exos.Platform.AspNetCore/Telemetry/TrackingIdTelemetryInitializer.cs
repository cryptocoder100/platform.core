using System;
using System.Diagnostics;
using System.Reflection;
using Exos.Platform.AspNetCore.Extensions;
using Exos.Platform.AspNetCore.Helpers;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace Exos.Platform.AspNetCore.Telemetry
{
    /// <summary>
    /// Add the request tracking ID (if present) to all Application Insights telemetry.
    /// </summary>
    public class TrackingIdTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackingIdTelemetryInitializer"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">An <see cref="IHttpContextAccessor" /> instance.</param>
        public TrackingIdTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <inheritdoc/>
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            var supportProperties = telemetry as ISupportProperties;
            var context = _httpContextAccessor?.HttpContext;
            if (context != null)
            {
                // Not all log statements are in the context of a request. This one is.
                var trackingId = context.GetTrackingId();
                if (string.IsNullOrEmpty(trackingId))
                {
                    // It's possible for Application Insights to start getting telemetry before the
                    // tracking ID middleware runs; e.g. in the Request event. However, since we
                    // want to include tracking ID with every bit of telemetry, we're going to cheat
                    // and create one now.

                    // NOTE. Access to HttpContext is not thread-safe and so we can't control whether another telemetry
                    // initializer is doing the same thing to the same context... we could lock the context, however, that
                    // might be a performance impact... so for now we'll leave this off with the understanding that we might not
                    // get a tracking ID with every single log entry. But since every request usually has more than a few
                    // log entries it should still be fairly easy to correlate related entries for those that do get it.
                    // trackingId = context.Request.Headers["Tracking-Id"] = Guid.NewGuid().ToString().ToLowerInvariant();
                }

                supportProperties.Properties["TrackingId"] = trackingId;
            }
            else
            {
                // Look for TrackingId in Activity.Current if httpContext is not present.
                if (Activity.Current != null && Activity.Current.Tags != null)
                {
                    var trackingId = Activity.Current.GetTrackingId();
                    if (!supportProperties.Properties.ContainsKey("TrackingId"))
                    {
                        supportProperties.Properties["TrackingId"] = trackingId;
                    }
                }
            }
        }
    }
}
