using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exos.Platform.AspNetCore.Helpers;
using Exos.Platform.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;

namespace Exos.Platform.AspNetCore.AppConfiguration
{
    internal sealed class ExosAzureAppConfigurationStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
                if (ConfigurationHelper.IsAppConfigurationEnabled(configuration))
                {
                    // Allows Azure App Configuration / Feature Management to refresh values at regular intervals
                    app.UseAzureAppConfiguration();
                }

                // Let others add middleware
                next(app);
            };
        }
    }
}
