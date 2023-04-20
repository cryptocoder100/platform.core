#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable CS0618 // Type or member is obsolete
namespace Exos.Platform.Messaging.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Authentication;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.Messaging.Helper;
    using Exos.Platform.Messaging.Policies;
    using Exos.Platform.Messaging.Repository.Model;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Polly;
    using Polly.Registry;

    /// <inheritdoc/>
    public class MqMessagePublisher : IMessagePublisher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Microsoft.Azure.ServiceBus.Primitives.ITokenProvider _tokenProvider;
        private readonly ILogger _logger;
        private readonly MessagePublisherOptions _messagePublisherOptions;
        private readonly MessageSection _messageSection;
        private readonly IFailoverPolicy<TopicClient> _topicFailoverPolicy;
        private readonly IFailoverPolicy<QueueClient> _queueFailoverPolicy;
        private readonly ExosClientEntityPool<TopicClient> _topicClientPool;
        private readonly ExosClientEntityPool<QueueClient> _queueClientPool;

        /// <summary>
        /// Initializes a new instance of the <see cref="MqMessagePublisher"/> class.
        /// </summary>
        /// <param name="serviceProvider">service provider.</param>
        /// <param name="logger">A logger for writing trace messages.</param>
        public MqMessagePublisher(IServiceProvider serviceProvider, ILogger<MqMessagePublisher> logger)
        {
            _serviceProvider = serviceProvider;
            if (_serviceProvider != null)
            {
                var options = _serviceProvider.GetService<IOptions<MessagePublisherOptions>>();
                if (options != null && options.Value != null)
                {
                    _messagePublisherOptions = options.Value;
                }

                var messageSectionOptions = _serviceProvider.GetService<IOptions<MessageSection>>();
                _messageSection = messageSectionOptions?.Value;
            }

            _tokenProvider = new Microsoft.Azure.ServiceBus.Primitives.ManagedIdentityTokenProvider(new ExosAzureServiceTokenProvider());
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _topicClientPool = new ExosTopicClientPool(_tokenProvider);
            _queueClientPool = new ExosQueueClientPool(_tokenProvider);
            _topicFailoverPolicy = new FailoverOnExcessiveTopicExceptionPolicy(_messageSection.FailoverConfig);
            _queueFailoverPolicy = new FailoverOnExcessiveQueueExceptionPolicy(_messageSection.FailoverConfig);
        }

        /// <inheritdoc/>
        public string Environment { get; set; }

        /// <summary>
        /// WriteToTopic.
        /// </summary>
        /// <param name="messageEntity">message entity.</param>
        /// <param name="brokerMessage">broker message.</param>
        /// <returns>Task.</returns>
        public async Task WriteToTopic(MessageEntity messageEntity, IList<Message> brokerMessage)
        {
            if (messageEntity == null)
            {
                throw new ArgumentNullException(nameof(messageEntity));
            }

            if (_messagePublisherOptions != null && _messagePublisherOptions.UseAliasFQDN)
            {
                // Using Alias/FQDN/Single Service Bus Url for both primary/secondry namespace
                await WriteToTopicUsingAliasFullyQualifieddDomainName(messageEntity, brokerMessage).ConfigureAwait(false);
            }
            else
            {
                // Using primary/secondry namespace seperatly
                await WriteToTopicUsingPrimarySecondaryNamespace(messageEntity, brokerMessage).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task WriteToQueue(MessageEntity messageEntity, IList<Message> brokerMessage)
        {
            if (messageEntity == null)
            {
                throw new ArgumentNullException(nameof(messageEntity));
            }

            QueueClient queueClient = null;

            try
            {
                queueClient = await _queueFailoverPolicy.EnsureExecutionFailoverAsync(_queueClientPool, messageEntity);
                await queueClient.SendAsync(brokerMessage).ConfigureAwait(false);
            }
            catch (Exception ex1)
            {
                _logger.LogError(MessagingLoggingEvent.PublishFailedToPrimary, ex1, "Publishing failed to Queue using Primary name-space.");
                try
                {
                    queueClient = await _queueFailoverPolicy.EnsureExecutionFailoverAsync(_queueClientPool, messageEntity, true, ex1);
                    await queueClient.SendAsync(brokerMessage).ConfigureAwait(false);
                }
                catch (Exception ex2)
                {
                    _logger.LogError(MessagingLoggingEvent.PublishFailedToPrimaryAndSecondary, ex2, "Publishing failed to queue using Primary and secondary name-space.");
                    throw new ExosMessagingException("Could not send message to Queue.", ex2);
                }
            }
            finally
            {
                try
                {
                    if (queueClient != null)
                    {
                        await queueClient.CloseAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(MessagingLoggingEvent.PublishFailedToPrimaryAndSecondary, e, "Queue close error.");
                }
            }
        }

        /// <summary>
        /// writes to event hub.
        /// </summary>
        /// <param name="messageEntity">message entity.</param>
        /// <param name="brokerMessage">brokermessage.</param>
        /// <returns>Task.</returns>
        public async Task WriteToEventHub(MessageEntity messageEntity, IList<EventData> brokerMessage)
        {
            if (messageEntity == null)
            {
                throw new ArgumentNullException(nameof(messageEntity));
            }

            // Find the ResiliencyPolicy
            IAsyncPolicy eventHubResiliencyPolicy = null;
            if (_serviceProvider != null)
            {
                IReadOnlyPolicyRegistry<string> registry = _serviceProvider.GetService<IReadOnlyPolicyRegistry<string>>();

                if (registry != null && registry.ContainsKey(EventHubsResiliencyPolicy.EventHubsResiliencyPolicyName))
                {
                    eventHubResiliencyPolicy = registry.Get<IAsyncPolicy>(EventHubsResiliencyPolicy.EventHubsResiliencyPolicyName);
                }
            }

            // Use the ResiliencyPolicy
            if (eventHubResiliencyPolicy != null)
            {
                await eventHubResiliencyPolicy.ExecuteAsync(async () =>
                {
                    await SendToEventHub(messageEntity, brokerMessage).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            else
            {
                // Just send with default ResiliencyPolicy
#pragma warning disable CA1062 // Validate arguments of public methods
                await SendToEventHub(messageEntity, brokerMessage).ConfigureAwait(false);
#pragma warning restore CA1062 // Validate arguments of public methods
            }
        }

        private static async Task SendToEventHub(MessageEntity messageEntity, IList<EventData> brokerMessage)
        {
            string partitionKey = GetPartitionKey(brokerMessage);
            EventHubClient connection;

            if (!MessageEntity.ConnectionStringHasKeys(messageEntity.EventHubConnectionString))
            {
                connection = EventHubConnectionPool.GetManagedConnection(messageEntity.EventHubNameSpace, messageEntity.EntityName, messageEntity.EventHubConnectionString);
            }
            else
            {
                connection = EventHubConnectionPool.GetConnection(messageEntity.EventHubNameSpace, messageEntity.EntityName, messageEntity.EventHubConnectionString);
            }

            // Batch the messages.
            if (brokerMessage.Count > 1)
            {
                var batchOptions = string.IsNullOrEmpty(partitionKey) ? new BatchOptions() { MaxMessageSize = 1000000 } : new BatchOptions() { MaxMessageSize = 1000000, PartitionKey = partitionKey };
                var batcher = connection.CreateBatch(batchOptions);
                int i = 0;
                do
                {
                    // || batcher.Size > maxBatchSize
                    if (!batcher.TryAdd(brokerMessage[i]))
                    {
                        // Time to send the batch.
                        await connection.SendAsync(batcher);
                        // Create new batcher.
                        batcher = connection.CreateBatch(batchOptions);
                    }

                    i++;
                }
                while (i < brokerMessage.Count);

                // Send the rest of the batch if any.
                if (batcher.Count > 0)
                {
                    await connection.SendAsync(batcher);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(partitionKey))
                {
                    await connection.SendAsync(brokerMessage, partitionKey);
                }
                else
                {
                    await connection.SendAsync(brokerMessage);
                }
            }
        }

        /// <summary>
        /// gets the partition key, if any.
        /// </summary>
        /// <param name="brokerMessage">broker message.</param>
        /// <returns>partition key value.</returns>
        private static string GetPartitionKey(IList<EventData> brokerMessage)
        {
            string partitionKeyValue = null;
            if (brokerMessage?.Any() != null)
            {
                var paryKey = brokerMessage.FirstOrDefault().Properties.Where(i => i.Key == MessageMetaData.PartitionKey).FirstOrDefault();

                if (!paryKey.Equals(default(KeyValuePair<string, object>)))
                {
                    partitionKeyValue = (string)paryKey.Value;
                }
            }

            return partitionKeyValue;
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-geo-dr
        /// Alias: The name for a disaster recovery configuration that you set up.
        /// The alias provides a single stable Fully Qualified Domain Name (FQDN) connection string.
        /// Applications use this alias connection string to connect to a namespace.
        /// Using an alias ensures that the connection string is unchanged when the failover is triggered.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task WriteToTopicUsingAliasFullyQualifieddDomainName(MessageEntity messageEntity, IList<Message> brokerMessage)
        {
            var nameSpace = messageEntity.ConnectionString;
            TopicClient topicClient = null;
            IAsyncPolicy serviceBusResiliencyPolicy = null;
            try
            {
                // Find the ResiliencyPolicy
                if (_serviceProvider != null)
                {
                    IReadOnlyPolicyRegistry<string> registry = _serviceProvider.GetService<IReadOnlyPolicyRegistry<string>>();
                    if (registry != null && registry.ContainsKey(ServiceBusResiliencyPolicy.ServiceBusResiliencyPolicyName))
                    {
                        serviceBusResiliencyPolicy = registry.Get<IAsyncPolicy>(ServiceBusResiliencyPolicy.ServiceBusResiliencyPolicyName);
                    }
                }

                // Use exos policy on top of Service bus default policy.
                if (serviceBusResiliencyPolicy != null)
                {
                    await serviceBusResiliencyPolicy.ExecuteAsync(async () =>
                    {
                        topicClient = _topicClientPool.GetClientEntity(nameSpace, messageEntity.EntityName, ExosTopicClientPool.GetRetryPolicy(messageEntity.MaxRetryCount));
                        await topicClient.SendAsync(brokerMessage).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                }
                else
                {
                    topicClient = _topicClientPool.GetClientEntity(nameSpace, messageEntity.EntityName, ExosTopicClientPool.GetRetryPolicy(messageEntity.MaxRetryCount));
                    await topicClient.SendAsync(brokerMessage).ConfigureAwait(false);
                }
            }
            catch (Exception ex1)
            {
                _logger.LogError(MessagingLoggingEvent.PublishFailedToPrimary, ex1, "Publishing failed to topic using alias/FQDN.");
            }
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-geo-dr.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task WriteToTopicUsingPrimarySecondaryNamespace(MessageEntity messageEntity, IList<Message> brokerMessage)
        {
            IAsyncPolicy serviceBusResiliencyPolicy = null;
            TopicClient topicClient = null;
            Func<Task> publishTask = async () => await topicClient.SendAsync(brokerMessage).ConfigureAwait(false);
            try
            {
                // Find the ResiliencyPolicy
                if (_serviceProvider != null)
                {
                    IReadOnlyPolicyRegistry<string> registry = _serviceProvider.GetService<IReadOnlyPolicyRegistry<string>>();
                    if (registry != null && registry.ContainsKey(ServiceBusResiliencyPolicy.ServiceBusResiliencyPolicyName))
                    {
                        serviceBusResiliencyPolicy = registry.Get<IAsyncPolicy>(ServiceBusResiliencyPolicy.ServiceBusResiliencyPolicyName);
                    }
                }

                topicClient = await _topicFailoverPolicy.EnsureExecutionFailoverAsync(_topicClientPool, messageEntity);

                // nameSpace
                if (serviceBusResiliencyPolicy != null)
                {
                    await serviceBusResiliencyPolicy.ExecuteAsync(publishTask).ConfigureAwait(false);
                }
                else
                {
                    await publishTask().ConfigureAwait(false);
                }
            }
            catch (Exception ex1)
            {
                _logger.LogError(MessagingLoggingEvent.PublishFailedToPrimary, ex1, "Publishing failed to topic using primary name-space.");

                try
                {
                    topicClient = await _topicFailoverPolicy.EnsureExecutionFailoverAsync(_topicClientPool, messageEntity, true, ex1);
                    // nameSpace
                    if (serviceBusResiliencyPolicy != null)
                    {
                        await serviceBusResiliencyPolicy.ExecuteAsync(publishTask).ConfigureAwait(false);
                    }
                    else
                    {
                        await publishTask().ConfigureAwait(false);
                    }
                }
                catch (Exception ex2)
                {
                    _logger.LogError(MessagingLoggingEvent.PublishFailedToPrimaryAndSecondary, "Publishing failed to topic using primary and secondary name-space.");
                    throw new ExosMessagingException("Could not send message to Topic.", ex2);
                }
            }
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CA1031 // Do not catch general exception types