#nullable enable
#pragma warning disable CA1031 // Do not catch general exception types

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace Exos.Platform.AspNetCore.Telemetry
{
    internal abstract class BaseTelemetryProcessor : ITelemetryProcessor
    {
        public BaseTelemetryProcessor(ITelemetryProcessor next, IHttpContextAccessor httpContextAccessor)
        {
            ArgumentNullException.ThrowIfNull(httpContextAccessor);

            Next = next; // May be null
            HttpContextAccessor = httpContextAccessor;
        }

        protected ITelemetryProcessor? Next { get; set; }

        protected IHttpContextAccessor HttpContextAccessor { get; set; }

        public void Process(ITelemetry item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (item is not ISupportProperties supportProperties)
            {
                // Not supported
                return;
            }

            try
            {
                var filter = Filter(item, supportProperties.Properties);
                if (!filter)
                {
                    // Run the next processor
                    Next?.Process(item);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                supportProperties.Properties["Telemetry.Exception"] = ex.ToString();

                // Do not rethrow; logging should not fail
            }
        }

        protected abstract bool Filter(ITelemetry telemetry, IDictionary<string, string> properties);
    }
}
