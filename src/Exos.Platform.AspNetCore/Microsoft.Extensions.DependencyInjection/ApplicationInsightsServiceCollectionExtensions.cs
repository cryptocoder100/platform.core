#pragma warning disable SA1600 // Elements should be documented

using System;
using System.Linq;
using Exos.Platform.AspNetCore.Middleware;
using Exos.Platform.AspNetCore.Telemetry;
using Exos.Platform.Telemetry;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering Application Insights services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class ApplicationInsightsServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a list of path prefixes to ignore from Application Insights telemetry.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to configure.</param>
        /// <param name="setupAction">An optional callback for configuring the provided <see cref="IgnoreTelemetryProcessor" />.</param>
        /// <returns>The configured <see cref="IServiceCollection" />.</returns>
        public static IServiceCollection AddExosIgnoreTelemetryProcessor(this IServiceCollection services, Action<IgnoreTelemetryProcessorOptions> setupAction = null)
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));

            services.AddApplicationInsightsTelemetryProcessor<IgnoreTelemetryProcessor>();

            if (setupAction != null)
            {
                services.Configure(setupAction);
            }

            return services;
        }

        /// <summary>
        /// Adds the request tracking ID (if present) to all Application Insights telemetry.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to configure.</param>
        /// <returns>The configured <see cref="IServiceCollection" />.</returns>
        /// <seealso cref="TrackingIdExtensions.UseTrackingId(AspNetCore.Builder.IApplicationBuilder)" />
        public static IServiceCollection AddExosTrackingIdTelemetryInitializer(this IServiceCollection services)
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));

            services.AddSingleton<ITelemetryInitializer, TrackingIdTelemetryInitializer>();
            return services;
        }

        /// <summary>
        /// Adds user info (if present) to all Application Insights telemetry.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to configure.</param>
        /// <returns>The configured <see cref="IServiceCollection" />.</returns>
        public static IServiceCollection AddExosUserInfoTelemetryInitializer(this IServiceCollection services)
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));

            services.AddSingleton<ITelemetryInitializer, UserInfoTelemetryInitializer>();
            return services;
        }

        [Obsolete("Calling AddExosPlatformDefaults now includes cloud name telemetry implicitly.")]
        public static IServiceCollection AddExosCloudRoleNameTelemetryInitializer(this IServiceCollection services)
        {
            return services.TryAddTelemetryInitializer<GlobalEnricherTelemetryInitializer>();
        }

        internal static IServiceCollection TryAddTelemetryInitializer<TImplementation>(this IServiceCollection services) where TImplementation : class, ITelemetryInitializer
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Allow multiple calls to register the same telemetry initializers by looking for an existing registration
            if (!services.Any(d => d.ServiceType == typeof(ITelemetryInitializer) && d.ImplementationType == typeof(TImplementation)))
            {
                services.AddSingleton<ITelemetryInitializer, TImplementation>();
            }

            return services;
        }
    }
}
