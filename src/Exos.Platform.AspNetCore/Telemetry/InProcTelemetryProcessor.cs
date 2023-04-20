using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Exos.Platform.AspNetCore.Telemetry
{
    internal sealed class InProcTelemetryProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;

        public InProcTelemetryProcessor(ITelemetryProcessor next)
        {
            _next = next; // May be null
        }

        public void Process(ITelemetry item)
        {
            if (item != null && item is DependencyTelemetry dependencyTelemetry)
            {
#pragma warning disable CA1310 // Specify StringComparison for correctness
                if (dependencyTelemetry.Type != null && dependencyTelemetry.Type.StartsWith("InProc"))
                {
                    // Ignore this telemetry by not passing it downstream
                    return;
                }
#pragma warning restore CA1310 // Specify StringComparison for correctness
            }

            // Call next processor
            _next?.Process(item);
        }
    }
}
