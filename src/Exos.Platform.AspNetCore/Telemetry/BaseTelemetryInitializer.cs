#pragma warning disable CA1031 // Do not catch general exception types

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Exos.Platform.AspNetCore.Extensions;
using Exos.Platform.AspNetCore.Helpers;
using Exos.Platform.AspNetCore.Telemetry;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace Exos.Platform.Telemetry
{
    internal abstract class BaseTelemetryInitializer : ITelemetryInitializer
    {
        public BaseTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor == null)
            {
                throw new ArgumentNullException(nameof(httpContextAccessor));
            }

            HttpContextAccessor = httpContextAccessor;
        }

        protected IHttpContextAccessor HttpContextAccessor { get; set; }

        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            if (telemetry is not ISupportProperties supportProperties)
            {
                // Not supported
                return;
            }

            try
            {
                Execute(telemetry, supportProperties.Properties);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                supportProperties.Properties["Telemetry.Exception"] = ex.ToString();

                // Do not rethrow; logging should not fail
            }
        }

        protected abstract void Execute(ITelemetry telemetry, IDictionary<string, string> properties);
    }
}
