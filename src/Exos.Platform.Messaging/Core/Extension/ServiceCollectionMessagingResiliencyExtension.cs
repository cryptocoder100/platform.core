namespace Exos.Platform.Messaging.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Exos.Platform.AspNetCore.Resiliency.Policies;
    using Exos.Platform.Messaging.Policies;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using Polly.Registry;

    /// <summary>
    /// Events hubs ResiliencyPolicy. This will let caller add this policy or some other appropriate policy.
    /// </summary>
    public static class ServiceCollectionMessagingResiliencyExtension
    {
        /// <summary>
        /// Add Messaging Resiliency Policy.
        /// </summary>
        /// <param name="services">services.</param>
        /// <param name="configuration">configuration.</param>
        /// <returns>Service Collection object.</returns>
        public static IServiceCollection AddMessagingResiliencyPolicy(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // Configure EventHubs options.
            services.Configure<EventHubsResiliencyPolicyOptions>(configuration.GetSection("ResiliencyPolicy:EventHubsResiliencyPolicyOptions"));

            // Add EventHub Svc Policy
            services.AddSingleton<IEventHubsResiliencyPolicy, EventHubsResiliencyPolicy>();

            // Configure ServiceBus options.
            services.Configure<ServiceBusResiliencyPolicyOptions>(configuration.GetSection("ResiliencyPolicy:ServiceBusResiliencyPolicyOptions"));

            // Add ServiceBusResiliencyPolicy Svc Policy
            services.AddSingleton<IServiceBusResiliencyPolicy, ServiceBusResiliencyPolicy>();
            return services;
        }

        /// <summary>
        /// Add Resilient Policies.
        /// </summary>
        /// <param name="serviceProvider">serviceProvider.</param>
        /// <param name="policies">policies.</param>
        /// <returns>IServiceProvider.</returns>
        public static IServiceProvider AddResilientPoliciesToPollyRegistry(this IServiceProvider serviceProvider, Dictionary<string, IResilientPolicy> policies)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (policies == null)
            {
                throw new ArgumentNullException(nameof(policies));
            }

            var registry = serviceProvider.GetService<IPolicyRegistry<string>>();
            if (registry == null)
            {
                throw new ArgumentNullException(null, "Could not find IPolicyRegistry, AddPlatformDefaults is mandatory in ConfigureServices method.");
            }

            foreach (var policy in policies)
            {
                var existingPolicy = registry.Where(i => i.Key == policy.Key).FirstOrDefault();
                if (existingPolicy.Equals(default(KeyValuePair<string, IsPolicy>)))
                {
                    registry.Add(policy.Key, policy.Value.ResilientPolicy);
                }
            }

            return serviceProvider;
        }

        /// <summary>
        /// Add Messaging Resilient Policies To Polly Registry.
        /// </summary>
        /// <param name="serviceProvider">serviceProvider.</param>
        /// <returns>IServiceProvider.</returns>
        public static IServiceProvider AddMessagingResilientPoliciesToPollyRegistry(this IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            var registry = serviceProvider.GetService<IPolicyRegistry<string>>();
            if (registry == null)
            {
                throw new ArgumentNullException(null, "Could not find IPolicyRegistry, AddPlatformDefaults is mandatory in ConfigureServices method.");
            }

            var messagingPolicies = new Dictionary<string, IResilientPolicy>();
            var eventHubPolicySvc = serviceProvider.GetService<IEventHubsResiliencyPolicy>();
            var serviceBusPolicySvc = serviceProvider.GetService<IServiceBusResiliencyPolicy>();

            if (eventHubPolicySvc != null)
            {
                messagingPolicies.Add(EventHubsResiliencyPolicy.EventHubsResiliencyPolicyName, eventHubPolicySvc);
            }

            if (serviceBusPolicySvc != null)
            {
                messagingPolicies.Add(ServiceBusResiliencyPolicy.ServiceBusResiliencyPolicyName, serviceBusPolicySvc);
            }

            if (messagingPolicies.Count > 0)
            {
                AddResilientPoliciesToPollyRegistry(serviceProvider, messagingPolicies);
            }

            return serviceProvider;
        }
    }
}
