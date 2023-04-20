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

    /// <summary>
    /// Consume messages from a Queue.
    /// </summary>
    internal class MessageQueueConsumer : MessageConsumer
    {
        private readonly ITokenProvider _tokenProvider;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageQueueConsumer"/> class.
        /// </summary>
        /// <param name="repository">IMessagingRepository.</param>
        /// <param name="environment">Running Environment.</param>
        /// <param name="logger">A logger for writing trace messages.</param>
        public MessageQueueConsumer(IMessagingRepository repository, string environment, ILogger<MessageQueueConsumer> logger) : base(repository, environment, logger)
        {
            _tokenProvider = new ManagedIdentityTokenProvider(new ExosAzureServiceTokenProvider());
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Adding listener to a queue by providing the Azure entity, incoming config and processor.
        /// </summary>
        /// <param name="config">MessageListenerConfig.</param>
        /// <param name="entity">MessageEntity.</param>
        /// <param name="clientProcessor">MessageProcessor.</param>
        /// <param name="namespaceType">Namespace type.</param>
        internal void AddQueueListener(MessageListenerConfig config, MessageEntity entity, MessageProcessor clientProcessor, MessageNamespaceType namespaceType)
        {
            if (config == null || entity == null || clientProcessor == null)
            {
                throw new ExosMessagingException(ExosMessagingConstant.ArgumentNull);
            }

            var numberOfThreads = config.NumberOfThreads == 0 ? ExosMessagingConstant.NumberOfThreads : config.NumberOfThreads;
            ClientProcessor = clientProcessor;
            ClientProcessorName = clientProcessor.GetType().Name;
            EntityName = entity.EntityName;
            DeliveryCount = config.RetryCount == 0 ? ExosMessagingConstant.RetryCount : config.RetryCount;
            NamespaceType = namespaceType;

            // Can move to constructor
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of Concurrent calls to the callback `ProcessMessagesAsync'
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = numberOfThreads,

                // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                // False value below indicates the Complete will be handled by the User Callback as seen in `ProcessMessagesAsync`.
                AutoComplete = false,
            };

            if (namespaceType == MessageNamespaceType.Secondary)
            {
                if (!MessageEntity.ConnectionStringHasKeys(entity.PassiveConnectionString))
                {
                    AzureClientEntity = new QueueClient(entity.PassiveConnectionString, entity.EntityName, _tokenProvider, TransportType.Amqp, ReceiveMode.PeekLock, GetRetryPolicy());
                }
                else
                {
                    AzureClientEntity = new QueueClient(entity.PassiveConnectionString, entity.EntityName, ReceiveMode.PeekLock, GetRetryPolicy());
                }

                AzureClientEntity.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
            }
            else
            {
                if (!MessageEntity.ConnectionStringHasKeys(entity.ConnectionString))
                {
                    AzureClientEntity = new QueueClient(entity.ConnectionString, entity.EntityName, _tokenProvider, TransportType.Amqp, ReceiveMode.PeekLock, GetRetryPolicy());
                }
                else
                {
                    AzureClientEntity = new QueueClient(entity.ConnectionString, entity.EntityName, ReceiveMode.PeekLock, GetRetryPolicy());
                }

                AzureClientEntity.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
            }

            // TODO: need to close the queue client when we shutdown?
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
