using System;
using System.Collections.Generic;
using System.Text;

namespace Exos.Platform.Telemetry
{
    internal sealed class RequestTelemetryFeature : IRequestTelemetryFeature
    {
        public string RedactedUrl { get; set; }

        public string RedactedReferrerUrl { get; set; }

        public string RedactedAuthorizationHeader { get; set; }

        public string RoutePattern { get; set; }

        public string TrackingId { get; set; }

        public IDictionary<string, string> RedactedHeaders { get; set; } = new Dictionary<string, string>();
    }
}
