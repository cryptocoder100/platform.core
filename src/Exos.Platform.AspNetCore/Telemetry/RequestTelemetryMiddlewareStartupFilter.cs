using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Exos.Platform.Telemetry
{
    internal sealed class RequestTelemetryMiddlewareStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                // Inject our middleware first
                app.UseMiddleware<RequestTelemetryMiddleware>();

                // Let others add middleware
                next(app);
            };
        }
    }
}
