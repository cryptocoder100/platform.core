using Exos.Platform.BuildInformation;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection" /> that configure the build information services.
    /// </summary>
    public static class BuildInformationServiceCollectionExtensions
    {
        /// <summary>
        /// Configures <see cref="IServiceCollection" /> to include build information.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> instance.</param>
        /// <returns>The <see cref="IServiceCollection" />.</returns>
        public static IServiceCollection AddExosBuildInformation(this IServiceCollection services)
        {
            services.AddSingleton<IBuildInformation, DefaultBuildInformation>();
            services.AddSingleton<ITelemetryInitializer, BuildInformationTelemetryInitializer>();

            return services;
        }
    }
}
