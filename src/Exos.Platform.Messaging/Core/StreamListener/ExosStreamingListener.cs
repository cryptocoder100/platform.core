#pragma warning disable CA1031 // Do not catch general exception types
namespace Exos.Platform.Messaging.Core.StreamListener
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.AspNetCore.Resiliency.Policies;
    using Exos.Platform.Messaging.Core.Listener;
    using Exos.Platform.Messaging.Helper;
    using Exos.Platform.Messaging.Repository;
    using Microsoft.Azure.EventHubs.Processor;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Polly;
    using Polly.Registry;

    /// <summary>
    /// this is the entry point for the apps to register with event hub streaming.
    /// </summary>
    public class ExosStreamingListener : IExosStreamingListener
    {
        private readonly IMessagingRepository _repository;
        private readonly EventingSection _section;
        private readonly int _leaseDuration = 60;
        private readonly int _renewInterval = 40;
        private ILoggerFactory _loggerFactory;
        private ILogger<ExosStreamingListener> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosStreamingListener"/> class.
        /// </summary>
        /// <param name="options">options.</param>
        /// <param name="loggerFactory">A logger factory for writing trace messages.</param>
        /// <param name="configuration">configuration.</param>
        public ExosStreamingListener(IOptions<EventingSection> options, ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<ExosStreamingListener>();

            _repository = new MessagingRepository(new MessagingDbContext(options.Value.MessageDb), configuration, _loggerFactory.CreateLogger<MessagingRepository>());
            _section = options.Value;
        }

        /// <summary>
        /// Registers the Listeners/EventProcessors.
        /// </summary>
        /// <param name="serviceProvider">service provider.</param>
        /// <param name="onRegistrationError">onRegistrationError.</param>
        /// <returns>Task.</returns>
        public async Task<List<EventProcessorHost>> RegisterListener(IServiceProvider serviceProvider, Action<string, Exception> onRegistrationError)
        {
            if (onRegistrationError == null)
            {
                throw new ArgumentNullException(nameof(onRegistrationError));
            }

            List<EventProcessorHost> eventProcessorHosts = new List<EventProcessorHost>();
            try
            {
                foreach (var listener in _section.Listeners)
                {
                    _logger.LogDebug($"Starting listener for Topic {listener.EntityName}, client processor {listener.Processor} and Subscription Name  {listener.SubscriptionName}");
                    if (listener.DisabledFlg)
                    {
                        _logger.LogError("Event processor SubscriptionName=" + listener.SubscriptionName + " is disabled, registration is not done/ignored.");
                        continue;
                    }

                    if (!string.IsNullOrEmpty(listener.SubscriptionName))
                    {
                        Type processorType = Type.GetType(listener.Processor, true, false);
                        ExosStreamProcessor processor;

                        processor = Activator.CreateInstance(processorType, serviceProvider) as ExosStreamProcessor;

                        if (processor == null)
                        {
                            throw new ExosMessagingException($"Processor can't be initialized {listener.Processor}");
                        }

                        processor.MessageConfigurationSection = _section;
                        EventProcessorHost eph = null;
                        eph = await RegisterStreamingistener(serviceProvider, listener, processor).ConfigureAwait(false);
                        eventProcessorHosts.Add(eph);
                        _logger.LogDebug($"Added listener for Topic {LoggerHelper.SanitizeValue(listener.EntityName)},client processor {LoggerHelper.SanitizeValue(listener.Processor)} and Subscription Name  {LoggerHelper.SanitizeValue(listener.SubscriptionName)}, Event Hub host details={LoggerHelper.SanitizeValue(eph)}");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Cannot register Event hub listeners.");
                onRegistrationError("Cannot register Event hub listeners.", e);
            }

            Console.WriteLine("Registering EventProcessor...");
            return eventProcessorHosts;
        }

        private PartitionManagerOptions GetPartitionManagerOptions()
        {
            return new PartitionManagerOptions() { LeaseDuration = TimeSpan.FromSeconds(_leaseDuration), RenewInterval = TimeSpan.FromSeconds(_renewInterval) };
        }

        private EventProcessorOptions GeEventProcessorOptions()
        {
            return new EventProcessorOptions() { MaxBatchSize = _section.MaxBatchSize, PrefetchCount = _section.MaxBatchSize * 2 };
        }

        private async Task<EventProcessorHost> RegisterStreamingistener(IServiceProvider serviceProvider, MessageListenerConfig listenerConfig, ExosStreamProcessor clientProcessor)
        {
            var messageEntities = _repository.GetMessageEntity(listenerConfig.EntityName, listenerConfig.EntityOwner);
            var entity = messageEntities.FirstOrDefault();
            if (entity == null)
            {
                _logger.LogError($"Azure Event hub entity not found {listenerConfig.EntityName}, {listenerConfig.EntityOwner}");
                throw new ExosMessagingException($"Azure Event hub entity not found {listenerConfig.EntityName}, {listenerConfig.EntityOwner}");
            }

            var eventProcessorHost = new EventProcessorHost(
                System.Environment.MachineName,
                listenerConfig.EntityName,
                listenerConfig.SubscriptionName,
                entity.EventHubConnectionString,
                listenerConfig.ConsumerBlobLocation,
                listenerConfig.ConsumerBlobContainerName);

            // Set PartitionManagerOptions options.
            eventProcessorHost.PartitionManagerOptions = GetPartitionManagerOptions();

            // Set eventProcessorOptions options.
            EventProcessorOptions eventProcessorOptions = GeEventProcessorOptions();

            // Find the ResiliencyPolicy
            IAsyncPolicy blobStorageResiliencyPolicy = null;
            IReadOnlyPolicyRegistry<string> registry = serviceProvider.GetRequiredService<IReadOnlyPolicyRegistry<string>>();
            if (registry != null && registry.ContainsKey(PolicyRegistryKeys.BlobStorageResiliencyPolicy))
            {
                blobStorageResiliencyPolicy = registry.Get<IAsyncPolicy>(PolicyRegistryKeys.BlobStorageResiliencyPolicy);
            }

            // Use the ResiliencyPolicy
            if (blobStorageResiliencyPolicy != null)
            {
                await blobStorageResiliencyPolicy.ExecuteAsync(async () =>
                {
                    await eventProcessorHost.RegisterEventProcessorFactoryAsync(new EventConsumerFactory(serviceProvider, clientProcessor, _repository, entity), eventProcessorOptions).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            else
            {
                await eventProcessorHost.RegisterEventProcessorFactoryAsync(new EventConsumerFactory(serviceProvider, clientProcessor, _repository, entity), eventProcessorOptions).ConfigureAwait(false);
            }

            return eventProcessorHost;
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types