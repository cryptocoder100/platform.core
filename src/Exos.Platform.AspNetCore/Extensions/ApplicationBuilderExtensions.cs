#pragma warning disable CA2201 // Do not raise reserved exception types

namespace Exos.Platform.AspNetCore.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using Exos.Platform.AspNetCore.Middleware;
    using Exos.Platform.AspNetCore.Resiliency.Policies;
    using Exos.Platform.AspNetCore.Security;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using Polly.Registry;
    using Prometheus;

    /// <summary>
    /// Extension methods for the IApplicationBuilder interface.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Provides a catch-app handler that will throw a <see cref="NotFoundException" />.
        /// </summary>
        /// <param name="app">The app to configure.</param>
        /// <param name="param">An optional name of the missing object's identifier parameter, e.g. 'Id'.</param>
        /// <param name="message">An optional user-friendly message explaining the error.</param>
        /// <returns>The configured app.</returns>
        [DebuggerNonUserCode]
        public static IApplicationBuilder RunNotFoundException(this IApplicationBuilder app, string param = null, string message = "The requested resource doesn't exist.")
        {
            // Catch-all
            app.Run(context =>
            {
                throw new NotFoundException(param, message);
            });

            return app;
        }

        /// <summary>
        /// An IApplicationBuilder extension method that add resilient policies to 'policies'.
        /// </summary>
        /// <param name="app"> The app to act on.</param>
        /// <param name="policies"> The policies.</param>
        /// <returns>app.</returns>
        public static IApplicationBuilder AddResilientPolicies(this IApplicationBuilder app, Dictionary<string, Policy> policies)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (policies == null)
            {
                throw new ArgumentNullException(nameof(policies));
            }

            var registry = app.ApplicationServices.GetService<IPolicyRegistry<string>>();
            if (registry == null)
            {
                throw new NullReferenceException("AddPlatformDefaults() is mandatory in ConfigureServices method.");
            }

            foreach (var policy in policies)
            {
                registry.Add(policy.Key, policy.Value);
            }

            return app;
        }

        /// <summary>
        /// An IApplicationBuilder extension method that adds resilient policies to 'policyTypes'.
        /// </summary>
        /// <param name="app"> The app to act on.</param>
        /// <param name="policies"> The policies.</param>
        /// <returns>app.</returns>
        public static IApplicationBuilder AddResilientPolicies(this IApplicationBuilder app, Dictionary<string, IResilientPolicy> policies)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (policies == null)
            {
                throw new ArgumentNullException(nameof(policies));
            }

            var registry = app.ApplicationServices.GetService<IPolicyRegistry<string>>();
            if (registry == null)
            {
                throw new NullReferenceException("AddPlatformDefaults() is mandatory in ConfigureServices method.");
            }

            foreach (var policy in policies)
            {
                registry.Add(policy.Key, policy.Value.ResilientPolicy);
            }

            return app;
        }

        /// <summary>
        /// Enable Prometheus metrics.
        /// https://github.com/prometheus-net/prometheus-net .
        /// </summary>
        /// <param name="app">The app to configure.</param>
        /// <returns>The configured app.</returns>
        public static IApplicationBuilder UseAppMetrics(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.UseHttpMetrics();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics();
            });

            return app;
        }

        /// <summary>
        /// Adds Application CORS policies per EXOS standards.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The <see cref="IApplicationBuilder" />.</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="app" /> is <b>null</b>.</exception>
        /// <remarks>
        /// Value is comma-delimited and not JSON appsettings array to allow ${TokenReplacement} to occur properly.
        /// AppConfiguration:  "ServiceCorsOrigins": "${Service:DefaultCorsOrigins}"
        /// Configuration Value for Service:DefaultCorsOrigins: "firstOrigin,secondOrigin".
        /// </remarks>
        public static IApplicationBuilder AddExosCorsPolicy(this IApplicationBuilder app, IConfiguration configuration = null)
        {
            _ = app ?? throw new ArgumentNullException(nameof(app));

            var corsOrigins = configuration?["ServiceCorsOrigins"].Split(',');

            app.UseCors(builder =>
            {
                builder
                    .SetIsOriginAllowed(host => true)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();

                if (corsOrigins?.Any() == true)
                {
                    builder
                        .SetIsOriginAllowedToAllowWildcardSubdomains()
                        .WithOrigins(corsOrigins.ToArray());
                }
            });

            return app;
        }

        /// <summary>
        /// Default configuration for application's request pipeline.
        /// Add the following Exos Middleware.
        /// ErrorHandlerMiddleware, TrackingIdMiddleware.
        /// Enable Swagger.
        /// </summary>
        /// <param name="app">The app<see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder AddExosPlatformDefaults(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var applicationName = Assembly.GetEntryAssembly().GetName().Name;

            // Enable our error handler middleware. We want this as early in the pipeline
            // as possible to catch any unhandled exceptions downstream.
            app.UseErrorHandler();

            // Serve index.html from root (necessary for Azure AlwaysOn)
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Forwarded headers
            app.TraceForwardedHeaders();
            app.UseForwardedHeaders();

            // Automatically add a Tracking-Id header if not already present in the request
            // and make it available for downstream HttpClient calls.
            app.UseTrackingId();

            // Enable routes/controllers
            app.UseRouting();

            // CORS
            app.UseCors();

            // The user context middleware will make use of the configuration options set above
            // and process the Authorization request header with the assumption that it is a
            // user context token generated by the gateway.
            app.UseUserContext(new[] { "ApiKey" });

            // Enable swagger and its UI
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                // Name the endpoint appropriately and use a relative path from your service root
                options.SwaggerEndpoint("v1/swagger.json", $"{applicationName} API");
            });

            app.UseAppMetrics();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Add Resilient Policies
            app.AddResilientPolicies(new Dictionary<string, IResilientPolicy>
            {
                { PolicyRegistryKeys.Http, app.ApplicationServices.GetService<IHttpRequestResiliencyPolicy>() },
            });

            // Enable health checks
            app.UseHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = (check) => check.Tags.Contains("ready"),
            });

            app.UseHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = (check) => check.Tags.Contains("live"),
            });

            return app;
        }
    }
}
#pragma warning restore CA2201 // Do not raise reserved exception types