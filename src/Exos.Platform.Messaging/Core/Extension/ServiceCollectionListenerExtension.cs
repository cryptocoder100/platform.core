namespace Exos.Platform.Messaging.Core.Extension
{
    using System;
    using Exos.Platform.Messaging.Core.Listener;
    using Exos.Platform.Messaging.Telemetry;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    /// <summary>
    /// Configuration Extensions for Listeners.
    /// </summary>
    public static class ServiceCollectionListenerExtension
    {
        /// <summary>
        /// Gets or sets ServiceProvider.
        /// </summary>
        public static ServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// Gets or sets Configuration.
        /// </summary>
        public static IConfiguration Configuration { get; set; }

        /// <summary>
        /// Configure Azure Service Bus Listeners.
        /// </summary>
        /// <param name="services">IServiceCollection.</param>
        /// <param name="configuration">IConfiguration.</param>
        /// <returns>Service Collection.</returns>
        public static IServiceCollection ConfigureAzureServiceBusEntityListener(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.Configure<MessageSection>(configuration.GetSection("Messaging"));
            services.AddSingleton<IExosMessageListener, ExosMessageListener>(); // This is the package to listen api.
            services.AddScoped<IFailedMessageService, FailedMessageService>(); // This is for retry  failed messages support.
            services.AddSingleton<ITelemetryInitializer, MessageProcessorTelemetryInitializer>();

            return services;
        }

        /// <summary>
        /// Configure Azure Service Bus Publisher.
        /// </summary>
        /// <param name="services">IServiceCollection.</param>
        /// <param name="configuration">IConfiguration.</param>
        /// <returns>Service Collection.</returns>
        public static IServiceCollection ConfigureAzureServiceBusEntityPublisher(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.Configure<MessageSection>(configuration.GetSection("Messaging"));
            services.AddSingleton<IExosMessaging, ExosMessaging>();
            return services;
        }

        /// <summary>
        /// Configure Message Entity Listeners.
        /// </summary>
        /// <param name="services">IServiceCollection.</param>
        /// <param name="configuration">IConfiguration.</param>
        /// <returns>Service Collection.</returns>
        public static IServiceCollection ConfigureAllMessageEntityListeners(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.Configure<MessageSection>(configuration.GetSection("Messaging"));
            services.AddSingleton<IExosMessageListener, ExosMessageListener>(); // This is the package to listen api
            services.AddSingleton<ITelemetryInitializer, MessageProcessorTelemetryInitializer>();

            return services;
        }

        /// <summary>
        /// Configure a Failed Message Service.
        /// </summary>
        /// <param name="services">IServiceCollection.</param>
        /// <param name="configuration">IConfiguration.</param>
        /// <returns>Service Collection.</returns>
        public static IServiceCollection ConfigureFailedMessageService(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.Configure<MessageSection>(configuration.GetSection("Messaging"));
            services.AddSingleton<IFailedMessageService, FailedMessageService>();
            return services;
        }
    }
}
