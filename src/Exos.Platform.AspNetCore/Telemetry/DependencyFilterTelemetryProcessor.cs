using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace Exos.Platform.AspNetCore.Telemetry
{
    internal sealed class DependencyFilterTelemetryProcessor : BaseTelemetryProcessor
    {
        public DependencyFilterTelemetryProcessor(ITelemetryProcessor next, IHttpContextAccessor httpContextAccessor) : base(next, httpContextAccessor)
        {
        }

        protected override bool Filter(ITelemetry telemetry, IDictionary<string, string> properties)
        {
            if (!(telemetry is DependencyTelemetry dependencyTelemetry))
            {
                // Not dependency telemetry; do not filter
                return false;
            }

            if (dependencyTelemetry.Success == false)
            {
                // Always log failures; do not filter
                return false;
            }

            var filter =
                // FilterTokenValidation(dependencyTelemetry, properties) ||
                FilterManagedIdentity(dependencyTelemetry, properties);

            return filter;
        }

        private static bool FilterTokenValidation(DependencyTelemetry telemetry, IDictionary<string, string> properties)
        {
            if (string.Equals("Http", telemetry.Type, StringComparison.OrdinalIgnoreCase))
            {
                var target = telemetry.Target ?? string.Empty; // Make safe for comparisons
                if (target.Equals("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                else if (target.EndsWith(".b2clogin.com", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool FilterManagedIdentity(DependencyTelemetry telemetry, IDictionary<string, string> properties)
        {
            if (string.Equals("Http", telemetry.Type, StringComparison.OrdinalIgnoreCase))
            {
                var target = telemetry.Target ?? string.Empty; // Make safe for comparisons
                if (target.Equals("169.254.169.254", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
