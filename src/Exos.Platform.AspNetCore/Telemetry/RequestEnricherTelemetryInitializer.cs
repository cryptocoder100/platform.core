using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;

namespace Exos.Platform.Telemetry
{
    internal sealed class RequestEnricherTelemetryInitializer : BaseTelemetryInitializer
    {
        public RequestEnricherTelemetryInitializer(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }

        protected override void Execute(ITelemetry telemetry, IDictionary<string, string> properties)
        {
            if (!(telemetry is RequestTelemetry requestTelemetry))
            {
                // Not request telemetry
                return;
            }

            EnrichWithRequestInformation(properties);
        }

        private void EnrichWithRequestInformation(IDictionary<string, string> properties)
        {
            var feature = HttpContextAccessor?.HttpContext?.Features?.Get<IRequestTelemetryFeature>();
            if (feature != null)
            {
                if (!string.IsNullOrEmpty(feature.RedactedReferrerUrl))
                {
                    properties["Request.Referrer"] = feature.RedactedReferrerUrl;
                }

                if (!string.IsNullOrEmpty(feature.RedactedAuthorizationHeader))
                {
                    properties["Request.Authorization"] = feature.RedactedAuthorizationHeader;
                }

                if (!string.IsNullOrEmpty(feature.RoutePattern))
                {
                    properties["Request.RoutePattern"] = feature.RoutePattern;
                }
            }
        }
    }
}
