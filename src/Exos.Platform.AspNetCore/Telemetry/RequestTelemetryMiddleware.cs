#nullable enable
#pragma warning disable CA1308 // Normalize strings to uppercase

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Exos.Platform.AspNetCore.Extensions;
using Exos.Platform.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Exos.Platform.Telemetry
{
    internal class RequestTelemetryMiddleware : IMiddleware
    {
        private static readonly Regex _guidRegex = new Regex("^[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}$", RegexOptions.Compiled);
        private static readonly Regex _authorizationHeaderRegex = new Regex("^(Basic|Bearer) (.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            var feature = new RequestTelemetryFeature();
            context.Features.Set<IRequestTelemetryFeature>(feature);

            // NOTE: Sequence is important because effects are cumulative
            CaptureUrl(context, feature);
            CaptureReferrerUrl(context, feature);
            CaptureAuthorizationHeader(context, feature);
            CaptureTrackingId(context, feature);
            CaptureHeaders(context, feature);

            context.Response.OnStarting(() =>
            {
                CaptureRoute(context, feature);
                return Task.CompletedTask;
            });

            return next(context);
        }

        private static void CaptureRoute(HttpContext context, IRequestTelemetryFeature feature)
        {
            // We don't enforce the order in which `UseRouting()` is called relative to other service
            // middleware, so the safest time to get the matched endpoint is when the response begins.
            Endpoint? endpoint = context.GetEndpoint();
            if (endpoint is RouteEndpoint routeEndpoint)
            {
                feature.RoutePattern = routeEndpoint.RoutePattern?.RawText;
            }
        }

        private static void CaptureUrl(HttpContext context, IRequestTelemetryFeature feature)
        {
            var url = context.Request.GetEncodedUrl();
            feature.RedactedUrl = UrlHelper.RedactUrl(url);
        }

        private static void CaptureReferrerUrl(HttpContext context, IRequestTelemetryFeature feature)
        {
            // NOTE: The misspelling of "Referer" is how it is written in the spec; we check for both
            var referrer = context.Request.Headers["Referer"];
            if (StringValues.IsNullOrEmpty(referrer))
            {
                referrer = context.Request.Headers["Referrer"];
            }

            if (!StringValues.IsNullOrEmpty(referrer))
            {
                feature.RedactedReferrerUrl = UrlHelper.RedactUrl(referrer);
            }
        }

        private static void CaptureAuthorizationHeader(HttpContext context, IRequestTelemetryFeature feature)
        {
            var authorization = context.Request.Headers["Authorization"];
            if (!StringValues.IsNullOrEmpty(authorization))
            {
                feature.RedactedAuthorizationHeader = RedactAuthorizationHeader(authorization);
            }
        }

        private static void CaptureTrackingId(HttpContext context, IRequestTelemetryFeature feature)
        {
            var trackingId = context.Request.Headers[TelemetryConstants.TrackingIdHeader];
            if (!StringValues.IsNullOrEmpty(trackingId))
            {
                // Do a little input validation...
                if (!_guidRegex.IsMatch(trackingId))
                {
                    // Force recreation
                    trackingId = StringValues.Empty;
                }
            }

            if (StringValues.IsNullOrEmpty(trackingId))
            {
                // Generate a new tracking ID
                trackingId = new StringValues(Guid.NewGuid().ToString().ToLowerInvariant());

                // Backfill request for downstream logic
                context.Request.Headers[TelemetryConstants.TrackingIdHeader] = trackingId;
            }

            feature.TrackingId = trackingId;

            // Also make available on the current Activity
            Activity.Current?.SetTrackingId(trackingId);
        }

        private static void CaptureHeaders(HttpContext context, IRequestTelemetryFeature feature)
        {
            foreach (var header in context.Request.Headers)
            {
                var name = header.Key?.ToLowerInvariant();
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                switch (name)
                {
                    case "cookie":
                        feature.RedactedHeaders[name] = TelemetryConstants.RedactedReplacement;
                        break;

                    case "authorization":
                        feature.RedactedHeaders[name] = feature.RedactedAuthorizationHeader;
                        break;

                    case "referer":
                    case "referrer":
                        feature.RedactedHeaders[name] = feature.RedactedReferrerUrl;
                        break;

                    default:
                        feature.RedactedHeaders[name] = header.Value;
                        break;
                }
            }
        }

        private static string RedactAuthorizationHeader(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var match = _authorizationHeaderRegex.Match(value);
            if (!match.Success)
            {
                // There is a value, but we can't parse it
                return TelemetryConstants.RedactedReplacement;
            }

            return $"{match.Groups[1].Value} {TelemetryConstants.RedactedReplacement}";
        }
    }
}
