#pragma warning disable CA1031 // Do not catch general exception types
namespace Exos.Platform.Messaging.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Authentication;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.Messaging.Helper;
    using Exos.Platform.Messaging.Repository;
    using Exos.Platform.Messaging.Repository.Model;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using Message = Microsoft.Azure.ServiceBus.Message;

    /// <inheritdoc/>
    public class ExosMessaging : IExosMessaging
    {
        private readonly IMessagingRepository _repository;
        private readonly IMessagePublisher _messageMessagePublisher;
        private readonly IServiceProvider _serviceProvider;
        private readonly Microsoft.Azure.ServiceBus.Primitives.ITokenProvider _tokenProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ExosMessaging> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosMessaging"/> class.
        /// </summary>
        /// <param name="options">Message Configuration.</param>
        /// <param name="loggerFactory">A logger factory for writing trace messages.</param>
        /// <param name="configuration">configuration.</param>
        /// <param name="serviceProvider">service provider.</param>
        public ExosMessaging(IOptions<MessageSection> options, ILoggerFactory loggerFactory, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<ExosMessaging>();

            _serviceProvider = serviceProvider;
            _repository = new MessagingRepository(new MessagingDbContext(options.Value.MessageDb), configuration, _loggerFactory.CreateLogger<MessagingRepository>());
            _messageMessagePublisher = new MqMessagePublisher(serviceProvider, _loggerFactory.CreateLogger<MqMessagePublisher>()) { Environment = options.Value.Environment };
            _logger.LogDebug($"Environment variable is {options.Value.Environment}");

            _tokenProvider = new Microsoft.Azure.ServiceBus.Primitives.ManagedIdentityTokenProvider(new ExosAzureServiceTokenProvider());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosMessaging"/> class.
        /// </summary>
        /// <param name="repository">IMessagingRepository.</param>
        /// <param name="serviceProvider">service provider.</param>
        /// <param name="loggerFactory">A logger factory for writing trace messages.</param>
        public ExosMessaging(IMessagingRepository repository, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<ExosMessaging>();

            _serviceProvider = serviceProvider;
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _messageMessagePublisher = new MqMessagePublisher(serviceProvider, loggerFactory.CreateLogger<MqMessagePublisher>());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosMessaging"/> class.
        /// </summary>
        /// <param name="repository">IMessagingRepository.</param>
        /// <param name="messageMessagePublisher">IMessagePublisher.</param>
        public ExosMessaging(IMessagingRepository repository, IMessagePublisher messageMessagePublisher)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _messageMessagePublisher = messageMessagePublisher;
        }

        /// <summary>
        /// Create the Azure service bus message entity.
        /// </summary>
        /// <param name="entity">entity.</param>
        /// <param name="incomingList">Exos Messages.</param>
        /// <returns>List of events.</returns>
        public static IList<EventData> CreateMessageEventHub(MessageEntity entity, IEnumerable<ExosMessage> incomingList)
        {
            if (incomingList == null)
            {
                throw new ExosMessagingException(ExosMessagingConstant.MessageObjectNull);
            }

            if (entity == null)
            {
                throw new ExosMessagingException(ExosMessagingConstant.ArgumentNull);
            }

            IList<EventData> brokerMessages = new List<EventData>();
            foreach (var incoming in incomingList)
            {
                var messageData = new AzureMessageData
                {
                    TopicName = entity.EntityName,
                    Message = incoming.MessageData,
                    MessageGuid = Guid.NewGuid(),
                    PublishTime = DateTime.Now,
                };
                if (messageData.Message != null && string.IsNullOrEmpty(messageData.Message.PublisherMessageUniqueId))
                {
                    messageData.Message.PublisherMessageUniqueId = Guid.NewGuid().ToString();
                }

#pragma warning disable CA2000 // Caller of this method is disposing the EventData Objects
                var brokerMessage = new EventData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageData)));
#pragma warning restore CA2000 // Caller of this method is disposing the EventData Objects

                if (incoming.MessageData.AdditionalMetaData != null && incoming.MessageData.AdditionalMetaData.Count > 0)
                {
                    foreach (var meta in incoming.MessageData.AdditionalMetaData)
                    {
                        if (!brokerMessage.Properties.ContainsKey(meta.DataFieldName))
                        {
                            brokerMessage.Properties.Add(meta.DataFieldName, meta.DataFieldValue);
                        }
                    }
                }

                if (!brokerMessage.Properties.ContainsKey(MessageMetaData.MessageGuid))
                {
                    brokerMessage.Properties.Add(MessageMetaData.MessageGuid, messageData.MessageGuid);
                }

                if (!brokerMessage.Properties.ContainsKey(MessageMetaData.CorrelationId))
                {
                    brokerMessage.Properties.Add(MessageMetaData.CorrelationId, messageData.Message.PublisherMessageUniqueId);
                }

                brokerMessages.Add(brokerMessage);
            }

            return brokerMessages;
        }

        /// <inheritdoc/>
        public async Task<int> PublishMessage(IList<ExosMessage> incomingMessages)
        {
            if (incomingMessages == null)
            {
                _logger.LogDebug(MessagingLoggingEvent.IncomingMessageObjectNull, "Incoming Messages is Empty");
                throw new ExosMessagingException(ExosMessagingConstant.MessageObjectNull);
            }

            var entityNames = incomingMessages.GroupBy(a => a.Configuration.EntityName)
                .Select(grp => grp.First())
                .ToList();

            // Each topicName
            foreach (var queue in entityNames)
            {
                var queueToPublishList = incomingMessages
                    .Where(a => a.Configuration.EntityName == queue.Configuration.EntityName).ToList();

                // This call should be from cache.
                var messageEntity = InitializeAzureEntities(queue.Configuration.EntityName, queue.Configuration.EntityOwner);
                if (messageEntity == null)
                {
                    throw new ExosMessagingException(ExosMessagingConstant.MessageEntityNotFound);
                }

                if (messageEntity.NameSpace == null)
                {
                    throw new ExosMessagingException(ExosMessagingConstant.NullConfiguration);
                }

                var brokerMessages = CreateMessage(messageEntity, queueToPublishList);

                // Check topic or queue initialize the subscription.
                if (string.Equals(messageEntity.ServiceBusEntityType, ExosMessagingConstant.AzureMessageEntityQueue, StringComparison.OrdinalIgnoreCase))
                {
                    await WriteToQueue(messageEntity, brokerMessages).ConfigureAwait(false);
                }
                else
                {
                    await WriteToTopic(messageEntity, brokerMessages).ConfigureAwait(false);
                }
            }

            return ExosMessagingConstant.SuccessCode;
        }

        /// <inheritdoc/>
        public Task<int> PublishMessageToQueue(ExosMessage incomingMessage)
        {
            if (incomingMessage == null)
            {
                _logger.LogDebug(MessagingLoggingEvent.IncomingMessageObjectNull, "Incoming Message is Empty");
                throw new ExosMessagingException(ExosMessagingConstant.NullConfiguration);
            }

            return PublishMessageToQueue(incomingMessage.Configuration, new List<ExosMessage> { incomingMessage });
        }

        /// <summary>
        /// Retrieve messages from the deadletter queue. Messages get remvoed from the queue after the call
        /// So the read messages must be kept on client side if needed for later processing.
        /// </summary>
        /// <param name="topicName">topic name.</param>
        /// <param name="topicOwnerName">topic owner name.</param>
        /// <param name="subscriptionName">subscription name.</param>
        /// <param name="batchCount"> This will maximum return batchsize*2 since we are looking at East and West.</param>
        /// Issue https://github.com/Azure/azure-service-bus-dotnet/issues/441
        /// <returns>Task.</returns>
        public async Task<List<ExosMessage>> ReadDlqMessages(string topicName, string topicOwnerName, string subscriptionName, int batchCount)
        {
            IMessageReceiver messageReceiverActive = null;
            IMessageReceiver messageReceiverPassive = null;
            try
            {
                batchCount = batchCount > 100 ? 100 : batchCount; // It is slow so limiting the batchsize
                var messageEntities = _repository.GetMessageEntity(topicName, topicOwnerName);
                var entity = messageEntities.FirstOrDefault();
                if (entity == null)
                {
                    _logger.LogError(MessagingLoggingEvent.TopicOrQueueNotFound, "Can't register listeners");
                    throw new ExosMessagingException($"Azure service bus entity not found {topicName}, {topicOwnerName}");
                }

                var dlqPath = "/$DeadLetterQueue";

                if (!MessageEntity.ConnectionStringHasKeys(entity.ConnectionString))
                {
                    messageReceiverActive = new MessageReceiver(entity.ConnectionString, EntityNameHelper.FormatSubscriptionPath(topicName, subscriptionName + dlqPath), _tokenProvider, Microsoft.Azure.ServiceBus.TransportType.Amqp, ReceiveMode.ReceiveAndDelete);
                }
                else
                {
                    // Legacy code for using connection strings with shared access keys
                    messageReceiverActive = new MessageReceiver(entity.ConnectionString, EntityNameHelper.FormatSubscriptionPath(topicName, subscriptionName + dlqPath), ReceiveMode.ReceiveAndDelete) { PrefetchCount = 100 };
                }

                // These messages are going to be processed at the caller level, so deleting the copy from the subscription.
                var bmessgagesActive = new List<Message>();
                for (var i = 0; i < batchCount; i++)
                {
                    bmessgagesActive.Add(await messageReceiverActive.ReceiveAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false));
                }

                if (!MessageEntity.ConnectionStringHasKeys(entity.PassiveConnectionString))
                {
                    messageReceiverActive = new MessageReceiver(entity.PassiveConnectionString, EntityNameHelper.FormatSubscriptionPath(topicName, subscriptionName + dlqPath), _tokenProvider, Microsoft.Azure.ServiceBus.TransportType.Amqp, ReceiveMode.ReceiveAndDelete);
                }
                else
                {
                    // Legacy code for using connection strings with shared access keys
                    messageReceiverPassive = new MessageReceiver(entity.PassiveConnectionString, EntityNameHelper.FormatSubscriptionPath(topicName, subscriptionName + dlqPath), ReceiveMode.ReceiveAndDelete) { PrefetchCount = 100 };
                }

                // These messages are going to be processed at the caller level, so deleting the copy from the subscription.
                var bmessgagesPassive = new List<Message>();
                for (var i = 0; i < batchCount; i++)
                {
                    bmessgagesPassive.Add(await messageReceiverPassive.ReceiveAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false));
                }

                var returnList = new List<ExosMessage>();
                PopulateMessageData(bmessgagesActive, returnList);
                PopulateMessageData(bmessgagesPassive, returnList);
                return returnList;
            }
            finally
            {
                if (messageReceiverActive != null)
                {
                    await messageReceiverActive.CloseAsync().ConfigureAwait(false);
                }

                if (messageReceiverPassive != null)
                {
                    await messageReceiverPassive.CloseAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Publish message to queue multiple messages. Pass the queue name and caller id in first parameter.
        /// </summary>
        /// <param name="configuration">configuration.</param>
        /// <param name="incomingMessages">incoming messages.</param>
        /// <returns>Task.</returns>
        public async Task<int> PublishMessageToQueue(MessageConfig configuration, IList<ExosMessage> incomingMessages)
        {
            if (incomingMessages == null)
            {
                _logger.LogDebug(MessagingLoggingEvent.IncomingMessageObjectNull, "Incoming Messages is Empty");
                throw new ExosMessagingException(ExosMessagingConstant.NullConfiguration);
            }

            // We have configuration in messages.
            if (configuration == null)
            {
                var queuNames = incomingMessages.GroupBy(a => a.Configuration.EntityName)
                    .Select(grp => grp.First())
                    .ToList();

                // Each topicName
                foreach (var queue in queuNames)
                {
                    var queueToPublishList = incomingMessages
                        .Where(a => a.Configuration.EntityName == queue.Configuration.EntityName).ToList();

                    // This call should be from cache.
                    var messageEntity = InitializeAzureEntities(queue.Configuration.EntityName, queue.Configuration.EntityOwner);
                    if (messageEntity == null)
                    {
                        throw new ExosMessagingException(ExosMessagingConstant.MessageEntityNotFound);
                    }

                    var brokerMessages = CreateMessage(messageEntity, queueToPublishList);

                    // Create the Topic and initialize the subscription.
                    await WriteToQueue(messageEntity, brokerMessages).ConfigureAwait(false);
                }

                return ExosMessagingConstant.SuccessCode;
            }
            else
            {
                var messageEntity = InitializeAzureEntities(configuration.EntityName, configuration.EntityOwner);
                var brokerMessages = CreateMessage(messageEntity, incomingMessages);

                // Create the Topic and initialize the subscription.
                return await WriteToQueue(messageEntity, brokerMessages).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public Task<int> PublishMessageToTopic(ExosMessage incomingMessage)
        {
            // _logger.LogDebug($"Incoming Message {LoggerHelper.SanitizeValue(incomingMessage)}");

            // ValidateTheTopic and status is active
            if (incomingMessage == null || incomingMessage.Configuration == null
                || incomingMessage.MessageData == null)
            {
                _logger.LogDebug(MessagingLoggingEvent.IncomingMessageObjectNull, "Incoming Message is Empty");
                throw new ExosMessagingException(ExosMessagingConstant.MessageObjectNull);
            }

            var messageEntity = InitializeAzureEntities(incomingMessage.Configuration.EntityName, incomingMessage.Configuration.EntityOwner);
            if (messageEntity == null)
            {
                // _logger.LogDebug($"Incoming Message has invalid Service bus entity {LoggerHelper.SanitizeValue(incomingMessage)}");
                throw new ExosMessagingException(ExosMessagingConstant.MessageEntityNotFound);
            }

            if (messageEntity.IsPublishToServiceBusActive == null || messageEntity.IsPublishToServiceBusActive == true)
            {
                // Else create and initialize and set the status
                // Publish the incomingMessage to azure and send successful incomingMessage
                // Write the db if publish fails and throw appropriate error code
                // if db fails then throw the error.
                var brokerMessages = CreateMessage(messageEntity, new List<ExosMessage> { incomingMessage });

                // Create the Topic and initialize the subscription.
                return WriteToTopic(messageEntity, brokerMessages);
            }
            else
            {
                // _logger.LogDebug($"Incoming Message has disabled entity publish to Service Bus, Payload is {LoggerHelper.SanitizeValue(incomingMessage)}");
                return Task.FromResult(ExosMessagingConstant.SuccessCode);
            }
        }

        /// <summary>
        /// Send group of messages to the topic. if all messages belong to one entity, then leave each message object
        /// configuration property to null. Else populate each message. This way caller does not need to sort the messages.
        /// </summary>
        /// <param name="configuration">configuration.</param>
        /// <param name="incomingMessages">incoming messages.</param>
        /// <returns>Task.</returns>
        public async Task<int> PublishMessageToTopic(MessageConfig configuration, IList<ExosMessage> incomingMessages)
        {
            // Add messages with single correlation id and one transaction
            if (configuration == null)
            {
                // Means get each entity owner from the data field
                // Assumption, owner should be same for all the topic since client will be same
                var distinctTopics = incomingMessages.GroupBy(a => a.Configuration.EntityName)
                    .Select(grp => grp.First())
                    .ToList();

                // Each topicName
                foreach (var distinctTopic in distinctTopics)
                {
                    var topictoPublishList = incomingMessages
                        .Where(a => a.Configuration.EntityName == distinctTopic.Configuration.EntityName).ToList();

                    // This call should be from cache.
                    var messageEntity = InitializeAzureEntities(distinctTopic.Configuration.EntityName, distinctTopic.Configuration.EntityOwner);
                    if (messageEntity == null)
                    {
                        throw new ExosMessagingException(ExosMessagingConstant.MessageEntityNotFound);
                    }

                    var brokerMessages = CreateMessage(messageEntity, topictoPublishList);

                    // Create the Topic and initialize the subscription.
                    await WriteToTopic(messageEntity, brokerMessages).ConfigureAwait(false);
                }

                return ExosMessagingConstant.SuccessCode;
            }
            else
            {
                if (incomingMessages == null)
                {
                    throw new ArgumentNullException(nameof(incomingMessages));
                }

                var messageEntity = InitializeAzureEntities(configuration.EntityName, configuration.EntityOwner);
                var brokerMessages = CreateMessage(messageEntity, incomingMessages);

                // Create the Topic and initialize the subscription.
                return await WriteToTopic(messageEntity, brokerMessages).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public bool ValidateAndInitializeAzureEntities(string entityName, string entityOwner)
        {
            // alidate the topic and initialize , throw error if initialize fails else send success
            var result = InitializeAzureEntities(entityName, entityOwner);
            return result != null;
        }

        /// <summary>
        /// Initialize the topic or queue if it is not already.
        /// </summary>
        /// <param name="entityName">Topic or Queue name.</param>
        /// <param name="entityOwner"> Micro service or the module which owns the topic/queue.</param>
        /// <returns>MessageEntity.</returns>
        public MessageEntity InitializeAzureEntities(string entityName, string entityOwner)
        {
            // Get the MessageEntity from the db
            // Check it exists in azure, if not create,
            // Check the subscription exists if not create.
            IList<MessageEntity> messageEntities;
            try
            {
                messageEntities = _repository.GetMessageEntity(entityName, entityOwner);
                if (messageEntities == null || messageEntities.Count == 0)
                {
                    _logger.LogDebug(MessagingLoggingEvent.TopicOrQueueNotFound, $"Azure Entity not found {LoggerHelper.SanitizeValue(entityName)}, {LoggerHelper.SanitizeValue(entityOwner)}.");
                    throw new ExosMessagingException(ExosMessagingConstant.MessageEntityNotFound);
                }

                foreach (var msgEntity in messageEntities)
                {
                    if (msgEntity.Status.ToUpperInvariant() != ExosMessagingConstant.AzureEntityStatusActive)
                    {
                        // TODO future implementation
                        // var sqlFilterOnlySubscriptionClient =
                        // new SubscriptionClient(msgEntity.NameSpace, entityName, "");
                    }
                }

                // TODO future create the entities and subscriptions
                // TODO future Add rules.
            }
            catch (Exception e)
            {
                _logger.LogError(MessagingLoggingEvent.GenericExceptionInMessaging, e, "Error in looking for azure entities in DB.");
                return null;
            }

            // Since subscription and filter is the only difference, we can send the first object back for
            // getting the configuration.
            return messageEntities.FirstOrDefault();
        }

        /// <inheritdoc/>
        public Task<int> PublishMessageToEventHub(ExosMessage incomingMessage)
        {
            // _logger.LogDebug($"Incoming Message: {LoggerHelper.SanitizeValue(incomingMessage)}");

            // ValidateTheTopic and status is active
            if (incomingMessage == null || incomingMessage.Configuration == null
                || incomingMessage.MessageData == null)
            {
                _logger.LogDebug(MessagingLoggingEvent.IncomingMessageObjectNull, "Incoming Message is Empty");
                throw new ExosMessagingException(ExosMessagingConstant.MessageObjectNull);
            }

            var messageEntity = InitializeAzureEntities(incomingMessage.Configuration.EntityName, incomingMessage.Configuration.EntityOwner);
            if (messageEntity == null)
            {
                // _logger.LogDebug($"Incoming Message has invalid Service bus entity: {LoggerHelper.SanitizeValue(incomingMessage)}");
                throw new ExosMessagingException(ExosMessagingConstant.MessageEntityNotFound);
            }

            if (messageEntity.IsPublishToEventHubActive != null && messageEntity.IsPublishToEventHubActive == true)
            {
                // Else create and initialize and set the status
                // Publish the incomingMessage to azure and send successful incomingMessage
                // Write the db if publish fails and throw appropriate error code
                // If db fails then throw the error.
                var brokerMessages = CreateMessageEventHub(messageEntity, new List<ExosMessage> { incomingMessage });

                // Create the Topic and initialize the subscription.
                var returnValue = WriteToEventHub(messageEntity, brokerMessages);

                foreach (var brokerMessage in brokerMessages)
                {
                    brokerMessage.Dispose();
                }

                return returnValue;
            }
            else
            {
                // _logger.LogDebug($"Incoming Message has disabled entity publish to EventHub, Payload is {LoggerHelper.SanitizeValue(incomingMessage)}");
                return Task.FromResult(ExosMessagingConstant.SuccessCode);
            }
        }

        /// <inheritdoc/>
        public async Task<IList<ExosMessage>> PublishMessages(IList<ExosMessage> incomingMessages)
        {
            if (incomingMessages == null)
            {
                _logger.LogDebug(MessagingLoggingEvent.IncomingMessageObjectNull, "Incoming Messages is Empty");
                throw new ExosMessagingException(ExosMessagingConstant.MessageObjectNull);
            }

            var entityNames = incomingMessages.GroupBy(a => a.Configuration.EntityName)
                .Select(grp => grp.First())
                .ToList();

            List<ExosMessage> failedMessages = new List<ExosMessage>();
            int returnCode = 0;
            // Each topicName
            foreach (var queue in entityNames)
            {
                var queueToPublishList = incomingMessages
                    .Where(a => a.Configuration.EntityName == queue.Configuration.EntityName).ToList();

                // This call should be from cache.
                var messageEntity = InitializeAzureEntities(queue.Configuration.EntityName, queue.Configuration.EntityOwner);
                if (messageEntity == null)
                {
                    throw new ExosMessagingException(ExosMessagingConstant.MessageEntityNotFound);
                }

                if (messageEntity.NameSpace == null)
                {
                    throw new ExosMessagingException(ExosMessagingConstant.NullConfiguration);
                }

                var brokerMessages = CreateMessage(messageEntity, queueToPublishList);

                // Check topic or queue initialize the subscription.
                if (string.Equals(messageEntity.ServiceBusEntityType, ExosMessagingConstant.AzureMessageEntityQueue, StringComparison.OrdinalIgnoreCase))
                {
                    returnCode = await WriteToQueue(messageEntity, brokerMessages).ConfigureAwait(false);
                    if (returnCode != ExosMessagingConstant.SuccessCode)
                    {
                        failedMessages.AddRange(queueToPublishList);
                    }
                }
                else
                {
                    returnCode = await WriteMessagesToTopic(messageEntity, brokerMessages).ConfigureAwait(false);
                    if (returnCode != ExosMessagingConstant.SuccessCode)
                    {
                        failedMessages.AddRange(queueToPublishList);
                    }
                }
            }

            return failedMessages;
        }

        /// <summary>
        /// Add Message Data to Exos Message.
        /// </summary>
        /// <param name="brokerMessages">List of Message.</param>
        /// <param name="returnList">List of ExosMessage.</param>
        private static void PopulateMessageData(IEnumerable<Message> brokerMessages, ICollection<ExosMessage> returnList)
        {
            foreach (var brokerMessage in brokerMessages)
            {
                var message = new ExosMessage();
                var body = Encoding.UTF8.GetString(brokerMessage.Body);
                message.MessageData = new MessageData
                {
                    Payload = body,
                    AdditionalMetaData = new List<MessageMetaData>(),
                };
                foreach (var props in brokerMessage.UserProperties)
                {
                    var messageData = new MessageMetaData
                    {
                        DataFieldName = props.Key,
                        DataFieldValue = props.Value.ToString(),
                    };
                    message.MessageData.AdditionalMetaData.Add(messageData);
                }

                message.MessageData.PublisherMessageUniqueId = brokerMessage.MessageId;
                returnList.Add(message);
            }
        }

        /// <summary>
        /// Create the Azure service bus message entity.
        /// </summary>
        /// <param name="entity">MessageEntity.</param>
        /// <param name="incomingList">Exos Messages.</param>
        /// <returns>List of Message.</returns>
        private static IList<Message> CreateMessage(MessageEntity entity, IEnumerable<ExosMessage> incomingList)
        {
            IList<Message> brokerMessages = new List<Message>();
            foreach (var incoming in incomingList)
            {
                var messageData = new AzureMessageData
                {
                    TopicName = entity.EntityName,
                    Message = incoming.MessageData,
                    MessageGuid = Guid.NewGuid(),
                    PublishTime = DateTime.Now,
                };
                var brokerMessage = new Message { Label = incoming.Configuration.EntityOwner }; // Message sender.

                if (incoming.MessageData.PublisherMessageUniqueId != null)
                {
                    brokerMessage.CorrelationId = incoming.MessageData.PublisherMessageUniqueId;
                }

                // Wrap the incomingMessage with message guid
                brokerMessage.MessageId = messageData.MessageGuid.ToString(); // for duplicate message detection
                brokerMessage.Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageData));
                brokerMessage.ContentType = "application/json"; // For future implementation.

                if (incoming.MessageData.AdditionalMetaData != null)
                {
                    foreach (var filter in incoming.MessageData.AdditionalMetaData)
                    {
                        if (filter.DataFieldName != null && filter.DataFieldName == MessageMetaData.ScheduleEnqueueDelayTime)
                        {
                            if (double.TryParse(filter.DataFieldValue, out var enqueueDelayTimeInSeconds))
                            {
                                brokerMessage.ScheduledEnqueueTimeUtc = DateTime.Now.AddSeconds(enqueueDelayTimeInSeconds);
                            }
                        }
                        else if (filter.DataFieldName != null && filter.DataFieldName == MessageMetaData.TimeToLive)
                        {
                            if (double.TryParse(filter.DataFieldValue, out var timeToLive))
                            {
                                brokerMessage.TimeToLive = TimeSpan.FromDays(timeToLive > 0 ? timeToLive : MessageMetaData.TimeToLiveDefault);
                            }
                        }

                        brokerMessage.UserProperties.Add(filter.DataFieldName, filter.DataFieldValue);
                    }
                }

                brokerMessages.Add(brokerMessage);
            }

            return brokerMessages;
        }

        private List<bool> WriteToErrorMessageLog(Exception e, MessageEntity messageEntity, IList<Message> brokerMessages)
        {
            // Write to the failed log. TODO make it batch more than 10.
            _logger.LogError(MessagingLoggingEvent.WritingToAzureServiceBusFailed, e, "Writing to the azure service bus entity failed.");

            List<bool> dbflg = new List<bool>();
            foreach (var brokerMessage in brokerMessages)
            {
                var log = new PublishErrorMessageLog
                {
                    Comments = e.Message,
                    FailedDateTime = DateTime.Now,
                    Status = ExosMessagingConstant.AzureMessagePublishStatusFailed,
                    MessageEntityName = messageEntity.EntityName,
                    TransactionId = brokerMessage.CorrelationId,
                    MessageGuid = Guid.Parse(brokerMessage.MessageId),
                    Payload = Encoding.UTF8.GetString(brokerMessage.Body),
                    MetaData = JsonConvert.SerializeObject(brokerMessage.UserProperties),
                    Publisher = brokerMessage.Label,
                };

                dbflg.Add(_repository.Add(log));
            }

            return dbflg;
        }

        /// <summary>
        /// With one topic configuration , write messages.
        /// </summary>
        /// <param name="messageEntity">message entity.</param>
        /// <param name="brokerMessages">broker messages.</param>
        /// <returns>Task.</returns>
        private async Task<int> WriteToTopic(MessageEntity messageEntity, IList<Message> brokerMessages)
        {
            // Added for unexpected issues.
            if (messageEntity == null || messageEntity.ConnectionString == null)
            {
                throw new ExosMessagingException(ExosMessagingConstant.NullConfiguration);
            }

            try
            {
                await _messageMessagePublisher.WriteToTopic(messageEntity, brokerMessages).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Should not execute based on polly resiliency
                // Write to error log.
                var writeToDbStatus = WriteToErrorMessageLog(ex, messageEntity, brokerMessages);
                bool insertStatus = false;

                // only dealing with one error message for now to avoid breaking changes
                if (writeToDbStatus != null && writeToDbStatus.Count == 1)
                {
                    insertStatus = writeToDbStatus[0];
                }

                if (!insertStatus)
                {
                    return ExosMessagingConstant.WriteToDbFailedCode; // This will let the caller retry.
                }
                else
                {
                    return ExosMessagingConstant.SuccessCode; // This will let the caller move onto the next Message.
                }
            }

            return ExosMessagingConstant.SuccessCode;
        }

        private async Task<int> WriteMessagesToTopic(MessageEntity messageEntity, IList<Message> brokerMessages)
        {
            // Added for unexpected issues.
            if (messageEntity == null || messageEntity.ConnectionString == null)
            {
                throw new ExosMessagingException(ExosMessagingConstant.NullConfiguration);
            }

            try
            {
                await _messageMessagePublisher.WriteToTopic(messageEntity, brokerMessages).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                WriteToErrorMessageLog(ex, messageEntity, brokerMessages);
                return ExosMessagingConstant.WriteFailedCode;
            }

            return ExosMessagingConstant.SuccessCode;
        }

        /// <summary>
        /// Writing to the queue.
        /// </summary>
        /// <param name="messageEntity">MessageEntity.</param>
        /// <param name="brokerMessages">List of Messages.</param>
        /// <returns>Return ExosMessagingConstant.SucessCode or ExosMessagingConstant.WriteFailedCode.</returns>
        private async Task<int> WriteToQueue(MessageEntity messageEntity, IList<Message> brokerMessages)
        {
            try
            {
                await _messageMessagePublisher.WriteToQueue(messageEntity, brokerMessages).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(MessagingLoggingEvent.WritingToAzureServiceBusFailed, e, "Writing to the azure service bus entity failed.");

                // Write to the failed log. TODO make it batch more than 10.
                foreach (var brokerMessage in brokerMessages)
                {
                    var log = new PublishErrorMessageLog
                    {
                        Comments = e.Message,
                        FailedDateTime = DateTime.Now,
                        Status = ExosMessagingConstant.AzureMessagePublishStatusFailed,
                        MessageEntityName = messageEntity.EntityName,
                        TransactionId = brokerMessage.CorrelationId,
                        MessageGuid = Guid.Parse(brokerMessage.MessageId),
                        Payload = Encoding.UTF8.GetString(brokerMessage.Body),
                        Publisher = brokerMessage.Label,
                    };

                    _repository.Add(log);
                }

                return ExosMessagingConstant.WriteFailedCode;
            }

            return ExosMessagingConstant.SuccessCode;
        }

        /// <summary>
        /// Write to error message log.
        /// </summary>
        /// <param name="exception">exception.</param>
        /// <param name="messageEntity">message entity.</param>
        /// <param name="brokerMessages">broker messages.</param>
        private void WriteToErrorMessageLog(Exception exception, MessageEntity messageEntity, IList<EventData> brokerMessages)
        {
            {
                // Write to the failed log. TODO make it batch more than 10.
                _logger.LogError(MessagingLoggingEvent.WritingToAzureEventHubFailed, exception, "Writing to the azure event hub entity failed.");

                // Write to the failed log. TODO make it batch more than 10.
                _logger.LogError(MessagingLoggingEvent.WritingToAzureEventHubFailed, exception, "Writing to the azure event hub entity failed.");
                foreach (var brokerMessage in brokerMessages)
                {
                    var transactionId = brokerMessage.Properties.FirstOrDefault(i => i.Key == MessageMetaData.CorrelationId);
                    var messageGuid = brokerMessage.Properties.FirstOrDefault(i => i.Key == MessageMetaData.MessageGuid);
                    var log = new PublishErrorMessageLog
                    {
                        Comments = exception.Message,
                        FailedDateTime = DateTime.Now,
                        Status = ExosMessagingConstant.AzureMessagePublishStatusFailed,
                        MessageEntityName = messageEntity.EntityName,
                        TransactionId = transactionId.GetValue(),
                        MessageGuid = Guid.Parse(messageGuid.Value.ToString()),
                        Payload = Encoding.UTF8.GetString(brokerMessage.Body),
                        MetaData = JsonConvert.SerializeObject(brokerMessage.Properties),
                        Publisher = messageEntity.Owner,
                    };

                    _repository.Add(log);
                }
            }
        }

        /// <summary>
        /// With one topic configuration , write messages.
        /// </summary>
        /// <param name="messageEntity">message entity.</param>
        /// <param name="brokerMessages">broker messages.</param>
        /// <returns>Task.</returns>
        private async Task<int> WriteToEventHub(MessageEntity messageEntity, IList<EventData> brokerMessages)
        {
            // Added for unexpected issues.
            if (messageEntity == null || messageEntity.NameSpace == null)
            {
                throw new ExosMessagingException(ExosMessagingConstant.NullConfiguration);
            }

            try
            {
                await _messageMessagePublisher.WriteToEventHub(messageEntity, brokerMessages).ConfigureAwait(false);
            }
            catch (SocketException socketException)
            {
                WriteToErrorMessageLog(socketException, messageEntity, brokerMessages);
                return ExosMessagingConstant.WriteFailedCode; // This will let the caller retry.
            }
            catch (EventHubsException eventHubsException)
            {
                // Write to error log.
                WriteToErrorMessageLog(eventHubsException, messageEntity, brokerMessages);
                if (eventHubsException.IsTransient)
                {
                    return ExosMessagingConstant.WriteFailedCode; // This will let the caller retry.
                }
                else
                {
                    return ExosMessagingConstant.SuccessCode; // This will let the caller move onto the next Message.
                }
            }
            catch (Exception e)
            {
                {
                    WriteToErrorMessageLog(e, messageEntity, brokerMessages);
                    return ExosMessagingConstant.SuccessCode; // Will let called move to next message.
                }
            }

            return ExosMessagingConstant.SuccessCode;
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types
