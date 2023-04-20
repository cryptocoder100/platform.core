#pragma warning disable CA1506
using System;
using System.Reflection;
using Exos.Platform.AspNetCore.Extensions;
using Exos.Platform.AspNetCore.Middleware;
using Exos.Platform.Messaging.Core.Extension;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ListenerServiceSample
{
    /// <summary>
    /// Start up.
    /// </summary>
    public class Startup
    {
        private static readonly string ApplicationName = Assembly.GetEntryAssembly().GetName().Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="environment">Web host environment.</param>
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        /// <summary>
        /// Gets configuration.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Gets web host environment.
        /// </summary>
        public IWebHostEnvironment Environment { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">Application builder.</param>
        public static void Configure(IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            // Automatically add a Tracking-Id header if not already present in the request
            // and make it available for downstream HttpClient calls.
            app.UseTrackingId();

            // Enable swagger and its UI
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                // Name the endpoint appropriately and use a relative path from your service root
                options.SwaggerEndpoint("v1/swagger.json", $"{ApplicationName} API");
            });

            // Enable routes/controllers
            app.UseRouting();
            app.UseAppMetrics();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Enable health checks
            app.UseHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = (check) => check.Tags.Contains("live"),
            });

            app.UseHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = (check) => check.Tags.Contains("ready"),
            });

            // Start servicebus listeners
            app.StartAzureSbListeners(app?.ApplicationServices);

            // Throw a NotFoundException for and requests that are not handled by the middleware above
            app.RunNotFoundException();
        }

        /// <summary>
        /// Configure services.
        /// </summary>
        /// <param name="services">Service collection.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Applies the standard configuration for JSON serialization and configures all
            // controller actions to require authentication. Use the AllowAnonymous attribute on
            // controllers or actions you want anonymous access to. For more control over platform
            // defaults, see the code in the ServiceCollectionExtensions.AddPlatformDefaults method.
            services.AddExosPlatformDefaults(Configuration, Environment);

            // Configure service bus listener
            services.ConfigureAzureServiceBusEntityListener(Configuration);

            // Generate a Swagger documentation file
            services.AddSwagger();
            services.AddHealthChecks()
                .AddCheck("FatalException", () => HealthCheckResult.Healthy(), tags: new[] { "live" });
        }
    }
}
#pragma warning restore CA1506