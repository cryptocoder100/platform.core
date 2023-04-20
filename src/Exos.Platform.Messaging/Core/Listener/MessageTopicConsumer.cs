#pragma warning disable CS0618 // Type or member is obsolete
namespace Exos.Platform.Messaging.Core.Listener
{
    using System;
    using Exos.Platform.AspNetCore.Authentication;
    using Exos.Platform.Messaging.Helper;
    using Exos.Platform.Messaging.Repository;
    using Exos.Platform.Messaging.Repository.Model;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Primitives;
    using Microsoft.Extensions.Logging;

    /// <inheritdoc/>
    internal class MessageTopicConsumer : MessageConsumer
    {
        private readonly ITokenProvider _tokenProvider;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageTopicConsumer"/> class.
        /// </summary>
        /// <param name="repository">IMessagingRepository.</param>
        /// <param name="environment">Running environment.</param>
        /// <param name="logger">A logger for writing trace messages.</param>
        public MessageTopicConsumer(IMessagingRepository repository, string environment, ILogger<MessageTopicConsumer> logger) : base(repository, environment, logger)
        {
            _tokenProvider = new ManagedIdentityTokenProvider(new ExosAzureServiceTokenProvider());
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Adding a subscription listener to the subscription using
        /// incoming config, entity, subscription and processor.
        /// </summary>
        /// <param name="config">MessageListenerConfig.</param>
        /// <param name="entity">MessageEntity.</param>
        /// <param name="subscriptionName">Subscription Name.</param>
        /// <param name="clientProcessor">MessageProcessor.</param>
        /// <param name="namespaceType">Namespace type.</param>
        internal void AddTopicSubscriptionListener(
            MessageListenerConfig config,
            MessageEntity entity,
            string subscriptionName,
            MessageProcessor clientProcessor,
            MessageNamespaceType namespaceType)
        {
            if (config == null || entity == null || clientProcessor == null || subscriptionName == null)
            {
                throw new ExosMessagingException(ExosMessagingConstant.ArgumentNull);
            }

            // Can move to constructor
            DeliveryCount = config.RetryCount == 0 ? ExosMessagingConstant.RetryCount : config.RetryCount;
            ClientProcessor = clientProcessor;
            ClientProcessorName = clientProcessor.GetType().Name;
            EntityName = subscriptionName;
            NamespaceType = namespaceType;

            var numberOfThreads = config.NumberOfThreads == 0 ? ExosMessagingConstant.NumberOfThreads : config.NumberOfThreads;
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of Concurrent calls to the callback `ProcessMessagesAsync`,
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = numberOfThreads,

                // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                // False value below indicates the Complete will be handled by the User Callback as seen in `ProcessMessagesAsync`.
                AutoComplete = false,
                MaxAutoRenewDuration = TimeSpan.FromMinutes(5), // making it fixed.
            };

            // If we register against the same namespace in separate threads, we can get messagelockhost exception.
            // So if the primary namespace is equal to the PassiveNameSpace then ignore it.
            if (namespaceType == MessageNamespaceType.Secondary)
            {
                if (!MessageEntity.ConnectionStringHasKeys(entity.PassiveConnectionString))
                {
                    AzureClientEntity = new SubscriptionClient(entity.PassiveConnectionString, entity.EntityName, subscriptionName, _tokenProvider, TransportType.Amqp, ReceiveMode.PeekLock, GetRetryPolicy());
                }
                else
                {
                    AzureClientEntity = new SubscriptionClient(entity.PassiveConnectionString, entity.EntityName, subscriptionName, ReceiveMode.PeekLock, GetRetryPolicy());
                }

                AzureClientEntity.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
            }
            else
            {
                if (!MessageEntity.ConnectionStringHasKeys(entity.ConnectionString))
                {
                    AzureClientEntity = new SubscriptionClient(entity.ConnectionString, entity.EntityName, subscriptionName, _tokenProvider, TransportType.Amqp, ReceiveMode.PeekLock, GetRetryPolicy());
                }
                else
                {
                    AzureClientEntity = new SubscriptionClient(entity.ConnectionString, entity.EntityName, subscriptionName, ReceiveMode.PeekLock, GetRetryPolicy());
                }

                AzureClientEntity.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
            }
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
