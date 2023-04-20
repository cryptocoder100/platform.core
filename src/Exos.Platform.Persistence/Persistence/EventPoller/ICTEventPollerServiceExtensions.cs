#pragma warning disable CA1715 // Identifiers should have correct prefix
namespace Exos.Platform.Persistence.EventPoller
{
    using System;
    using System.Net.Http;
    using Exos.Platform.AspNetCore.Extensions;
    using Exos.Platform.AspNetCore.Resiliency.Policies;
    using Exos.Platform.AspNetCore.Security;
    using Exos.Platform.Persistence.EventTracking;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Options;
    using Polly;
    using Polly.Registry;

    /// <summary>
    /// Configure the ICT event poller service.
    /// </summary>
    public static class ICTEventPollerServiceExtensions
    {
        /// <summary>
        /// Configure the ICT event poller service.
        /// </summary>
        /// <typeparam name="T">Entity that implements EventTrackingEntity class.</typeparam>
        /// <param name="services">Service Collection.</param>
        /// <param name="configuration">Configuration properties.</param>
        public static void AddICTEventPollerService<T>(this IServiceCollection services, IConfiguration configuration) where T : EventTrackingEntity, new()
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            IConfigurationSection configurationSection = configuration.GetSection("ICTEventPollerService");
            services.Configure<EventPollerServiceSettings>(configurationSection);
            services.TryAddSingleton<IICTEventPollerService, ICTEventPollerService<T>>();
        }

        /// <summary>
        /// Configure the ICT event poller service for service bus.
        /// </summary>
        /// <typeparam name="T"><see cref="EventTrackingEntity"/>.</typeparam>
        /// <typeparam name="TCP"><see cref="EventPublishCheckPointEntity"/>.</typeparam>
        /// <param name="services"><see cref="IServiceCollection"/>.</param>
        /// <param name="configuration"><see cref="IConfiguration"/>.</param>
        public static void AddICTEventPollerServiceBusService<T, TCP>(this IServiceCollection services, IConfiguration configuration)
            where T : EventTrackingEntity, new()
            where TCP : EventPublishCheckPointEntity, new()
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var configurationSection = configuration.GetSection("ICTEventPollerService");
            services.Configure<EventPollerServiceSettings>(configurationSection);
            services.TryAddSingleton<IICTEventPollerCheckPointServiceBusService, ICTEventPollerServiceBusService<T, TCP>>();
        }

        /// <summary>
        /// Configure the ICT event poller service for event hub service.
        /// </summary>
        /// <typeparam name="T"><see cref="EventTrackingEntity"/>.</typeparam>
        /// <typeparam name="TCP"><see cref="EventPublishCheckPointEntity"/>.</typeparam>
        /// <param name="services"><see cref="IServiceCollection"/>.</param>
        /// <param name="configuration"><see cref="IConfiguration"/>.</param>
        public static void AddICTEventPollerEventHubService<T, TCP>(this IServiceCollection services, IConfiguration configuration)
           where T : EventTrackingEntity, new()
           where TCP : EventPublishCheckPointEntity, new()
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var configurationSection = configuration.GetSection("ICTEventPollerService");
            services.Configure<EventPollerServiceSettings>(configurationSection);
            services.TryAddSingleton<IICTEventPollerCheckPointEventHubService, ICTEventPollerEventHubService<T, TCP>>();
        }

        /// <summary>
        /// Configure the ICT event poller service for AzureTableStorage.
        /// </summary>
        /// <typeparam name="T"><see cref="EventTrackingEntity"/>.</typeparam>
        /// <typeparam name="TCP"><see cref="EventPublishCheckPointEntity"/>.</typeparam>
        /// <param name="services"><see cref="IServiceCollection"/>.</param>
        /// <param name="configuration"><see cref="IConfiguration"/>.</param>
        public static void AddICTEventPollerAzureTableStorageService<T, TCP>(this IServiceCollection services, IConfiguration configuration)
           where T : EventTrackingEntity, new()
           where TCP : EventPublishCheckPointEntity, new()
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var configurationSection = configuration.GetSection("ICTEventPollerService");
            services.Configure<EventPollerServiceSettings>(configurationSection);
            services.AddSingleton(typeof(ITableClientOperationsService<>), typeof(TableClientOperationsService<>));
            services.TryAddSingleton<IICTEventPollerCheckPointAzureTableStorageService, ICTEventPollerAzureTableStorageService<T, TCP>>();
        }

        /// <summary>
        /// Adds the ict event poller for the integrations service.
        /// </summary>
        /// <typeparam name="T"><see cref="EventTrackingEntity"/>.</typeparam>
        /// <typeparam name="TCP"><see cref="EventPublishCheckPointEntity"/>.</typeparam>
        /// <param name="services"><see cref="IServiceCollection"/>.</param>
        /// <param name="configuration"><see cref="IConfiguration"/>.</param>
        public static void AddICTEventPollerIntegrationsService<T, TCP>(this IServiceCollection services, IConfiguration configuration)
           where T : EventTrackingEntity, new()
           where TCP : EventPublishCheckPointEntity, new()
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var configurationSection = configuration.GetSection("ICTEventPollerService");
            services.Configure<EventPollerServiceSettings>(configurationSection);
            services.Configure<EventPollerIntegrationsServiceSettings>(configuration.GetSection("IntegrationsSvc"));

            var platformDefaults = new PlatformDefaultsOptions();
            configuration.GetSection("PlatformDefaults").Bind(platformDefaults);
            var noOpPolicyNative = Policy.NoOpAsync().AsAsyncPolicy<HttpResponseMessage>();
            services.AddHttpClient("Native")
                .ConfigurePrimaryHttpMessageHandler(configure => new DefaultHttpClientHandler(platformDefaults)).EnrichWithTenancySubdomain()
                .SetHandlerLifetime(TimeSpan.FromMinutes(
                    configuration.GetValue<int>("ResiliencyPolicy:HttpRequestPolicyOptions:HandlerLifetimeInMinutes")))
                .AddPolicyHandler((sp, request) =>
                {
                    var options = sp.GetService<IOptions<HttpRequestPolicyOptions>>().Value;
                    return !options.IsDisabled && options.RetryHttpMethod.Contains(request.Method)
                    ? sp.GetService<IReadOnlyPolicyRegistry<string>>().Get<IAsyncPolicy<HttpResponseMessage>>(PolicyRegistryKeys.Http)
                    : noOpPolicyNative;
                });

            services.TryAddSingleton<IICTEventPollerCheckPointIntegrationsService, ICTEventPollerIntegrationsService<T, TCP>>();
        }
    }
}
#pragma warning restore CA1715 // Identifiers should have correct prefix