namespace Exos.Platform.Persistence.EventPoller
{
    using System;
    using Exos.Platform.Persistence.EventTracking;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Configure the ICT event poller Hosted service.
    /// </summary>
    public static class ICTEventPollerHostedServiceExtensions
    {
        /// <summary>
        /// Configure the ICT event poller Hosted service.
        /// </summary>
        /// <typeparam name="T">Entity that implements EventTrackingEntity class.</typeparam>
        /// <param name="services">Service Collection.</param>
        /// <param name="configuration">Configuration properties.</param>
        public static void AddICTEventPollerHostedService<T>(this IServiceCollection services, IConfiguration configuration) where T : EventTrackingEntity, new()
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            IConfigurationSection configurationSection = configuration.GetSection("ICTEventPollerService");
            services.Configure<EventPollerServiceSettings>(configurationSection);
            services.TryAddSingleton<IHostedService, ICTEventPollerHostedService<T>>();
        }
    }
}
