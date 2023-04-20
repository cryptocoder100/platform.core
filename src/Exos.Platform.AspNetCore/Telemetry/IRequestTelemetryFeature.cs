#pragma warning disable SA1600 // Elements should be documented

using System;
using System.Collections.Generic;
using System.Text;

namespace Exos.Platform.Telemetry
{
    internal interface IRequestTelemetryFeature
    {
        string RedactedUrl { get; set; }

        string RedactedReferrerUrl { get; set; }

        string RedactedAuthorizationHeader { get; set; }

        string RoutePattern { get; set; }

        string TrackingId { get; set; }

        IDictionary<string, string> RedactedHeaders { get; set; }
    }
}
