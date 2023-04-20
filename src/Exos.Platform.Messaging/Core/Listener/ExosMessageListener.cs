#pragma warning disable CA1031 // Do not catch general exception types
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Exos.Platform.Messaging.Helper;
using Exos.Platform.Messaging.Repository;
using Exos.Platform.Messaging.Repository.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Exos.Platform.Messaging.Core.Listener
{
    /// <inheritdoc/>
    public class ExosMessageListener : IExosMessageListener
    {
        private readonly IMessagingRepository _repository;
        private readonly MessageSection _section;
        private readonly ConcurrentDictionary<string, MessageConsumer> _messageConsumers;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosMessageListener"/> class.
        /// </summary>
        /// <param name="options">Message Options.</param>
        /// <param name="loggerFactory">A logger factory for writing trace messages.</param>
        /// <param name="configuration">configuration.</param>
        public ExosMessageListener(IOptions<MessageSection> options, ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<ExosMessageListener>();

            _messageConsumers = new ConcurrentDictionary<string, MessageConsumer>();
            _repository = new MessagingRepository(new MessagingDbContext(options.Value.MessageDb), configuration, loggerFactory.CreateLogger<MessagingRepository>());
            _section = options.Value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosMessageListener"/> class.
        /// </summary>
        /// <param name="repository">IMessagingRepository.</param>
        public ExosMessageListener(IMessagingRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosMessageListener"/> class.
        /// </summary>
        /// <param name="options">Message Options.</param>
        /// <param name="loggerFactory">A logger factory for writing trace messages.</param>
        /// <param name="repository">IMessagingRepository.</param>
        public ExosMessageListener(IOptions<MessageSection> options, ILoggerFactory loggerFactory, IMessagingRepository repository)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<ExosMessageListener>();

            _messageConsumers = new ConcurrentDictionary<string, MessageConsumer>();
            _section = options.Value;
            _repository = repository;
        }

        /// <inheritdoc/>
        public ICollection<MessageConsumer> RegisterServiceBusEntityListener(MessageListenerConfig listenerConfig, MessageProcessor clientProcessor)
        {
            if (listenerConfig == null)
            {
                _logger.LogError(MessagingLoggingEvent.IncomingMessageObjectNull, "Listener configuration null");
                throw new ExosMessagingException("Configuration null exception");
            }

            return string.IsNullOrEmpty(listenerConfig.SubscriptionName) ?
                RegisterQueueListener(listenerConfig, clientProcessor) :
                RegisterTopicSubscriptionListener(listenerConfig, clientProcessor);
        }

        /// <inheritdoc/>
        public void StartListener()
        {
            StartListener(null);
        }

        /// <inheritdoc/>
        public void StartListener(IServiceProvider serviceProvider)
        {
            try
            {
                foreach (var listener in _section.Listeners)
                {
                    _logger.LogDebug($"Starting listener for Topic {listener.EntityName}, client processor {listener.Processor} and Subscription Name  {listener.SubscriptionName}");
                    if (listener.DisabledFlg)
                    {
                        _logger.LogError(MessagingLoggingEvent.SubscriptionLogEntryFailed, listener.SubscriptionName);
                        continue;
                    }

                    if (!string.IsNullOrEmpty(listener.SubscriptionName))
                    {
                        Type processorType = Type.GetType(listener.Processor, true, false);
                        MessageProcessor processor;
                        if (serviceProvider == null)
                        {
                            processor = Activator.CreateInstance(processorType) as MessageProcessor; // Default constructor
                        }
                        else
                        {
                            processor = Activator.CreateInstance(processorType, serviceProvider) as MessageProcessor;
                        }

                        if (processor == null)
                        {
                            throw new ExosMessagingException($"Processor can't be initialized {listener.Processor}");
                        }

                        processor.MessageConfigurationSection = _section;
                        processor.MessageConfigurationSection.MessageDb = _repository.Connection.ConnectionString;

                        // TODO add the pointer to thread safe collection so that we can respond to the stop.
                        // Sync /lock?
                        foreach (var messageConsumer in RegisterServiceBusEntityListener(listener, processor))
                        {
                            _messageConsumers.TryAdd($"{listener.EntityName}:{listener.SubscriptionName}:{messageConsumer.NamespaceType}", messageConsumer);
                        }

                        _logger.LogDebug($"Added listener for Topic {listener.EntityName},client processor {listener.Processor} and Subscription Name  {listener.SubscriptionName}");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(MessagingLoggingEvent.CantRegisterListener, e, "Can't register listeners");
            }
        }

        /// <inheritdoc/>
        public void StartAllEntityListeners(IServiceProvider serviceProvider)
        {
            try
            {
                var listenerConfig = _section.Listeners[0]; // Always one or the first

                // Get all the message entities
                var allEntities = _repository.GetAll<MessageEntity>();
                foreach (var entity in allEntities)
                {
                    if (entity.Status != "ACTIVE")
                    {
                        _logger.LogError(MessagingLoggingEvent.SubscriptionLogEntryFailed, entity.EntityName);
                        continue;
                    }

                    var processorType = Type.GetType(listenerConfig.Processor, true, false);
                    MessageProcessor processor;
                    if (serviceProvider == null)
                    {
                        processor = Activator.CreateInstance(processorType) as MessageProcessor; // Default constructor
                    }
                    else
                    {
                        processor = Activator.CreateInstance(processorType, serviceProvider) as MessageProcessor;
                    }

                    if (processor == null)
                    {
                        throw new ExosMessagingException($"Processor can't be initialized {listenerConfig.Processor}");
                    }

                    processor.MessageConfigurationSection = _section;
                    processor.MessageConfigurationSection.MessageDb = _repository.Connection.ConnectionString;

                    var listener = new MessageListenerConfig
                    {
                        EntityName = entity.EntityName,
                        DisabledFlg = listenerConfig.DisabledFlg,
                        EntityOwner = entity.Owner,
                        NumberOfThreads = listenerConfig.NumberOfThreads,
                        SubscriptionName = listenerConfig.SubscriptionName,
                    };

                    // TODO: Add the pointer to thread safe collection so that we can respond to the stop.
                    // Sync /lock?
                    foreach (var messageConsumer in RegisterServiceBusEntityListener(listener, processor))
                    {
                        _messageConsumers.TryAdd($"{listener.EntityName}:{listener.SubscriptionName}:{messageConsumer.NamespaceType}", messageConsumer);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(MessagingLoggingEvent.CantRegisterListener, e, "Can't register listeners");
            }
        }

        /// <inheritdoc/>
        public void StartEntityListener(IServiceProvider serviceProvider, string topicName, string subscriptionName)
        {
            string entityUniqueId = $"{topicName}:{subscriptionName}";
            _messageConsumers.TryGetValue($"{entityUniqueId}:{MessageNamespaceType.Primary}", out MessageConsumer messagePrimaryConsumer);
            if (messagePrimaryConsumer == null)
            {
                // Get the listener config
                var listenerConfig = _section.Listeners.Where(a => a.EntityName == topicName && a.SubscriptionName == subscriptionName).FirstOrDefault();
                var processorType = Type.GetType(listenerConfig.Processor, true, false);
                MessageProcessor processor;
                if (serviceProvider == null)
                {
                    processor = Activator.CreateInstance(processorType) as MessageProcessor; // Default constructor
                }
                else
                {
                    processor = Activator.CreateInstance(processorType, serviceProvider) as MessageProcessor;
                }

                if (processor == null)
                {
                    throw new ExosMessagingException($"Processor can't be initialized {listenerConfig.Processor}");
                }

                processor.MessageConfigurationSection = _section;
                processor.MessageConfigurationSection.MessageDb = _repository.Connection.ConnectionString;
                var listener = new MessageListenerConfig
                {
                    EntityName = listenerConfig.EntityName,
                    DisabledFlg = listenerConfig.DisabledFlg,
                    EntityOwner = listenerConfig.EntityOwner,
                    NumberOfThreads = listenerConfig.NumberOfThreads,
                    SubscriptionName = listenerConfig.SubscriptionName,
                };

                foreach (var registerdConsumer in RegisterServiceBusEntityListener(listener, processor))
                {
                    _messageConsumers.TryAdd($"{entityUniqueId}:{registerdConsumer.NamespaceType}", registerdConsumer);
                }
            }
        }

        /// <inheritdoc/>
        public void StartAllEntityListeners()
        {
            StartAllEntityListeners(null);
        }

        /// <inheritdoc/>
        public async Task StopListener()
        {
            foreach (var messageConsumer in _messageConsumers)
            {
                await messageConsumer.Value.AzureClientEntity.UnregisterMessageHandlerAsync(TimeSpan.FromSeconds(60)).ConfigureAwait(false);
            }

            _messageConsumers.Clear();
        }

        /// <inheritdoc/>
        public ICollection<string> GetActiveEntityListeners()
        {
            if (_messageConsumers != null)
            {
                return _messageConsumers.Keys;
            }

            return new List<string>();
        }

        /// <inheritdoc/>
        public async Task StopListener(string topicName, string subscriptionName)
        {
            // Stop both primary and secondary listeners if exist.
            await StopListener(topicName, subscriptionName, MessageNamespaceType.Primary);
            if (_section.FailoverConfig.IsFailoverEnabled)
            {
                await StopListener(topicName, subscriptionName, MessageNamespaceType.Secondary);
            }
        }

        /// <summary>
        /// Stop listener for the topic, subscription and namespace type specified.
        /// </summary>
        /// <param name="topicName">topic name.</param>
        /// <param name="subscriptionName">subscription name.</param>
        /// <param name="namespaceType">namespace type.</param>
        /// <returns><see cref="Task"/>.</returns>
        private async Task StopListener(string topicName, string subscriptionName, MessageNamespaceType namespaceType)
        {
            string entityUniqueId = $"{topicName}:{subscriptionName}:{namespaceType}";
            _messageConsumers.TryGetValue(entityUniqueId, out MessageConsumer messageConsumer);
            if (messageConsumer != null)
            {
                await messageConsumer.AzureClientEntity.UnregisterMessageHandlerAsync(TimeSpan.FromSeconds(60))
                    .ConfigureAwait(false);
                var removed = _messageConsumers.TryRemove(entityUniqueId, out MessageConsumer messageConsumerRemvoved);
                if (removed)
                {
                    _logger.LogTrace("Azure topic listner removed");
                }
            }
            else
            {
                _logger.LogTrace($"Azure topic listner not found {entityUniqueId}");
            }
        }

        /// <summary>
        /// Register a Queue Listener.
        /// </summary>
        /// <param name="listenerConfig">Listener Configuration.</param>
        /// <param name="clientProcessor">Message Processor.</param>
        private List<MessageConsumer> RegisterQueueListener(MessageListenerConfig listenerConfig, MessageProcessor clientProcessor)
        {
            var registeredQueueList = new List<MessageConsumer>();
            try
            {
                var messageEntities = _repository.GetMessageEntity(listenerConfig.EntityName, listenerConfig.EntityOwner);
                var entity = messageEntities.FirstOrDefault();
                if (entity == null)
                {
                    _logger.LogError(MessagingLoggingEvent.TopicOrQueueNotFound, $"Azure service bus entity not found {JsonSerializer.Serialize(listenerConfig.EntityName)}, {JsonSerializer.Serialize(listenerConfig.EntityOwner)}");
                    throw new ExosMessagingException($"Azure service bus entity not found {listenerConfig.EntityName}, {listenerConfig.EntityOwner}");
                }

                // Register listener to primary namespace
                var consumer = new MessageQueueConsumer(_repository, _section.Environment, _loggerFactory.CreateLogger<MessageQueueConsumer>());
                consumer.AddQueueListener(listenerConfig, entity, clientProcessor, MessageNamespaceType.Primary);
                registeredQueueList.Add(consumer);

                // Register listener to secondary namespace when failover enabled and namespace not the same
                if (_section.FailoverConfig.IsFailoverEnabled
                    && !MessagingHelper.AreSameNamespaces(entity.ConnectionString, entity.PassiveConnectionString))
                {
                    consumer = new MessageQueueConsumer(_repository, _section.Environment, _loggerFactory.CreateLogger<MessageQueueConsumer>());
                    consumer.AddQueueListener(listenerConfig, entity, clientProcessor, MessageNamespaceType.Secondary);
                    registeredQueueList.Add(consumer);
                }

                return registeredQueueList;
            }
            catch (Exception e)
            {
                _logger.LogError(MessagingLoggingEvent.CantRegisterListener, e, $"Error adding listener {JsonSerializer.Serialize(listenerConfig.EntityName)}, {JsonSerializer.Serialize(listenerConfig.EntityOwner)}, {JsonSerializer.Serialize(listenerConfig.Processor)}");
                throw new ExosMessagingException($"Error adding listener {listenerConfig.EntityName}, {listenerConfig.EntityOwner}, {listenerConfig.Processor}", e);
            }
        }

        /// <summary>
        /// Register Topic Listeners for primary and secondary namespaces.
        /// </summary>
        /// <param name="listenerConfig">Listener Configuration.</param>
        /// <param name="clientProcessor">Message Processor.</param>
        private List<MessageConsumer> RegisterTopicSubscriptionListener(MessageListenerConfig listenerConfig, MessageProcessor clientProcessor)
        {
            var registeredConsumerList = new List<MessageConsumer>();
            try
            {
                var messageEntities = _repository.GetMessageEntity(listenerConfig.EntityName, listenerConfig.EntityOwner);
                var entity = messageEntities.FirstOrDefault();
                if (entity == null)
                {
                    _logger.LogError(MessagingLoggingEvent.TopicOrQueueNotFound, $"Azure service bus entity not found {JsonSerializer.Serialize(listenerConfig.EntityName)}, {JsonSerializer.Serialize(listenerConfig.EntityOwner)}");
                    throw new ExosMessagingException($"Azure service bus entity not found {listenerConfig.EntityName}, {listenerConfig.EntityOwner}");
                }

                // Register listener to primary namespace
                registeredConsumerList.AddRange(RegisterTopicSubscriptionListener(listenerConfig, clientProcessor, entity, MessageNamespaceType.Primary));

                // Register listener to secondary namespace when failover enabled and namespace not the same
                if (_section.FailoverConfig.IsFailoverEnabled
                    && !MessagingHelper.AreSameNamespaces(entity.ConnectionString, entity.PassiveConnectionString))
                {
                    registeredConsumerList.AddRange(RegisterTopicSubscriptionListener(listenerConfig, clientProcessor, entity, MessageNamespaceType.Secondary));
                }

                return registeredConsumerList;
            }
            catch (Exception e)
            {
                _logger.LogError(MessagingLoggingEvent.CantRegisterListener, e, $"Error adding listener {JsonSerializer.Serialize(listenerConfig.EntityName)}, {JsonSerializer.Serialize(listenerConfig.EntityOwner)}, {JsonSerializer.Serialize(listenerConfig.Processor)}");
                throw new ExosMessagingException($"Error adding listener {listenerConfig.EntityName}, {listenerConfig.EntityOwner}, {listenerConfig.Processor}", e);
            }
        }

        /// <summary>
        /// Register a topic subscription listener to primary or secondary namespace.
        /// </summary>
        /// <param name="listenerConfig">Listener Configuration.</param>
        /// <param name="clientProcessor">Message Processor.</param>
        /// <param name="messageEntity">Message Entity.</param>
        /// <param name="namespaceType">Namespace type.</param>
        /// <returns><see cref="List{MessageConsumer}"/>.</returns>
        private List<MessageConsumer> RegisterTopicSubscriptionListener(MessageListenerConfig listenerConfig, MessageProcessor clientProcessor, MessageEntity messageEntity, MessageNamespaceType namespaceType)
        {
            var registeredConsumerList = new List<MessageConsumer>();

            var consumer = new MessageTopicConsumer(_repository, _section.Environment, _loggerFactory.CreateLogger<MessageTopicConsumer>());
            consumer.AddTopicSubscriptionListener(listenerConfig, messageEntity, listenerConfig.SubscriptionName, clientProcessor, namespaceType);
            registeredConsumerList.Add(consumer);
            int listenerInstantCount = listenerConfig.InstanceCount;
            // Adding Consumers based on instance count
            for (int i = listenerInstantCount - 1; i > 0; i--)
            {
                var mtc = new MessageTopicConsumer(_repository, _section.Environment, _loggerFactory.CreateLogger<MessageTopicConsumer>());
                mtc.AddTopicSubscriptionListener(listenerConfig, messageEntity, listenerConfig.SubscriptionName, clientProcessor, namespaceType);
                registeredConsumerList.Add(consumer);
            }

            return registeredConsumerList;
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types