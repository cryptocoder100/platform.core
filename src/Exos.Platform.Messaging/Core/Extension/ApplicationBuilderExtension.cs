namespace Exos.Platform.Messaging.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Exos.Platform.Messaging.Core.Listener;
    using Exos.Platform.Messaging.Core.StreamListener;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Azure.EventHubs.Processor;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Configure Azure Listeners.
    /// </summary>
    public static class ApplicationBuilderExtension
    {
        /// <summary>
        /// Start Azure Service Bus Listeners from configuration.
        /// </summary>
        /// <param name="app">IApplicationBuilder.</param>
        /// <param name="serviceProvider">IServiceProvider.</param>
        /// <returns>Start Azure Service Bus Listeners.</returns>
        public static IApplicationBuilder StartAzureSbListeners(this IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            serviceProvider.GetService<IExosMessageListener>().StartListener(serviceProvider);
            return app;
        }

        /// <summary>
        /// Stop Azure Service Bus Listeners from configuration.
        /// </summary>
        /// <param name="app">IApplicationBuilder.</param>
        /// <param name="serviceProvider">IServiceProvider.</param>
        /// <returns>Start Azure Service Bus Listeners.</returns>
        public static IApplicationBuilder StopAzureSbListeners(this IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            serviceProvider.GetService<IExosMessageListener>().StopListener();
            return app;
        }

        /// <summary>
        /// Starts Azure EventHub Listeners.
        /// </summary>
        /// <param name="serviceProvider">service provider.</param>
        /// <param name="onRegistrationError">on registration error.</param>
        /// <returns>Task.</returns>
        public static Task<List<EventProcessorHost>> StartAzureEventHubListeners(this IServiceProvider serviceProvider, Action<string, Exception> onRegistrationError)
        {
            return serviceProvider.GetService<IExosStreamingListener>().RegisterListener(serviceProvider, onRegistrationError);
        }
    }
}
