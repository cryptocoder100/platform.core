#pragma warning disable CA1031 // Do not catch general exception types
namespace Exos.Platform.Messaging.Core.Listener
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.Messaging.Helper;
    using Exos.Platform.Messaging.Repository;
    using Exos.Platform.Messaging.Repository.Model;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Message Consumer Base Class.
    /// </summary>
    public abstract class MessageConsumer
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageConsumer"/> class.
        /// </summary>
        /// <param name="repository">IMessagingRepository.</param>
        /// <param name="environment">Running environment.</param>
        /// <param name="logger">A logger for writing trace messages.</param>
        protected MessageConsumer(IMessagingRepository repository, string environment, ILogger logger)
        {
            MessagingRepository = repository;
            ServiceBusEnv = environment;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageConsumer"/> class.
        /// </summary>
        /// <param name="serviceProvider">IServiceProvider.</param>
        protected MessageConsumer(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets or sets IReceiverClient.
        /// </summary>
        internal IReceiverClient AzureClientEntity { get; set; }

        /// <summary>
        /// Gets or sets NamespaceType.
        /// </summary>
        internal MessageNamespaceType NamespaceType { get; set; }

        /// <summary>
        /// Gets or sets MessageProcessor.
        /// </summary>
        protected MessageProcessor ClientProcessor { get; set; }

        /// <summary>
        /// Gets or sets the type name of the <see cref="ClientProcessor" />.
        /// </summary>
        protected string ClientProcessorName { get; set; }

        /// <summary>
        /// Gets or sets IMessagingRepository.
        /// </summary>
        protected IMessagingRepository MessagingRepository { get; set; }

        /// <summary>
        /// Gets or sets ServiceProvider.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// Gets or sets EntityName.
        /// </summary>
        protected string EntityName { get; set; }

        /// <summary>
        /// Gets or sets ServiceBusEnv.
        /// </summary>
        protected string ServiceBusEnv { get; set; }

        /// <summary>
        /// Gets or sets DeliveryCount.
        /// </summary>
        protected int DeliveryCount { get; set; }

        /// <summary>
        /// Get Retry Policy.
        /// </summary>
        /// <returns>Return RetryPolicy.</returns>
        public static RetryPolicy GetRetryPolicy()
        {
            var retry = new RetryExponential(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), 10);
            return retry;
        }

        /// <summary>
        /// Need to call this when shutdown is happening from the container or windows service.
        /// </summary>
        /// <returns>Completed Task.</returns>
        public async Task Close()
        {
            await AzureClientEntity.CloseAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Exception handler.
        /// </summary>
        /// <param name="exceptionReceivedEventArgs">ExceptionReceivedEventArgs.</param>
        /// <returns>Completed Task.</returns>
        protected Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            if (exceptionReceivedEventArgs == null)
            {
                throw new ArgumentNullException(nameof(exceptionReceivedEventArgs));
            }

            var logAttributes = new Dictionary<string, object>();
            if (exceptionReceivedEventArgs?.ExceptionReceivedContext != null)
            {
                logAttributes["entityPath"] = LoggerHelper.SanitizeValue(exceptionReceivedEventArgs.ExceptionReceivedContext?.EntityPath);
                logAttributes["clientId"] = LoggerHelper.SanitizeValue(exceptionReceivedEventArgs.ExceptionReceivedContext?.ClientId);
                logAttributes["endpoint"] = LoggerHelper.SanitizeValue(exceptionReceivedEventArgs.ExceptionReceivedContext?.Endpoint);
                logAttributes["action"] = LoggerHelper.SanitizeValue(exceptionReceivedEventArgs.ExceptionReceivedContext?.Action);
            }

            using var scope = _logger.BeginScope(logAttributes);
            if (exceptionReceivedEventArgs.Exception != null)
            {
                _logger.LogError(exceptionReceivedEventArgs.Exception, "Error callback '{method}' is called.", nameof(ExceptionReceivedHandler));
            }
            else
            {
                _logger.LogError(MessagingLoggingEvent.GenericExceptionInMessaging, "Error callback '{method}' is called but no exception details found.", nameof(ExceptionReceivedHandler));
            }

            // TODO: add Logging
            return Task.CompletedTask;
        }

        /// <summary>
        /// Process message asynchronously.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Completed Task.</returns>
        protected async Task ProcessMessagesAsync(Message message, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var currentActivity = Activity.Current;
            if (currentActivity != null)
            {
                MessagingHelper.TryEnrichActivity(currentActivity, message.Body);

                // Items in the current activity Tags will automatically be placed in AI request telemetry Properties
                TelemetryHelper.TryEnrichActivity(currentActivity, KeyValuePair.Create("Message.Processor", ClientProcessorName));
                if (AzureClientEntity is SubscriptionClient subscriptionClient)
                {
                    TelemetryHelper.TryEnrichActivity(
                       currentActivity,
                       KeyValuePair.Create("Message.Subscription", subscriptionClient.SubscriptionName),
                       KeyValuePair.Create("Message.Topic", subscriptionClient.TopicPath));
                }

                var userProperties = message?.UserProperties;
                if (userProperties != null)
                {
                    if (userProperties.TryGetValue("EntityName", out var entityName))
                    {
                        TelemetryHelper.TryEnrichActivity(currentActivity, KeyValuePair.Create("Message.EntityName", entityName as string));
                    }

                    if (userProperties.TryGetValue("X-Client-Tag", out var clientTag))
                    {
                        TelemetryHelper.TryEnrichActivity(currentActivity, KeyValuePair.Create("X-Client-Tag", clientTag as string));
                    }

                    if (userProperties.TryGetValue("TrackingId", out var trackingId))
                    {
                        TelemetryHelper.TryEnrichActivity(currentActivity, KeyValuePair.Create("TrackingId", trackingId as string));
                    }
                }
            }

            var receivedTime = DateTime.Now;

            // Process the message
            // This can be done only if the queueClient is opened in ReceiveMode.PeekLock mode (which is default).
            if (message.UserProperties?.ContainsKey("MachineListenerName") == false)
            {
                _logger.LogDebug($"Adding machine name: {Environment.MachineName}");
                message.UserProperties.Add("MachineListenerName", Environment.MachineName);
            }

            message.UserProperties?.Add("messageSequence", message.SystemProperties.SequenceNumber);
            message.UserProperties?.Add("messageEnqueuedSequence", message.SystemProperties.EnqueuedSequenceNumber);
            var properties = JsonSerializer.Serialize(message.UserProperties);
            var messageProperty = new MessageProperty
            {
                Label = message.Label,
                CorrelationId = message.CorrelationId,
                MessageId = message.MessageId,
                MetaDataProperties = properties,
            };
            string messageBody = null;
            try
            {
                messageBody = Encoding.UTF8.GetString(message.Body);
                await ClientProcessor.Execute(messageBody, messageProperty).ConfigureAwait(false);

                // Complete the message so that it is not received again.
                await AzureClientEntity.CompleteAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
                var messageLog = new SubscriptionMessageLog
                {
                    SubscriptionName = EntityName,
                    MessageGuid = Guid.Parse(message.MessageId),
                    TransactionId = message.CorrelationId ?? Guid.Empty.ToString(),
                    ServiceBusEntityName = AzureClientEntity.Path.Substring(0, AzureClientEntity.Path.IndexOf("/", StringComparison.Ordinal)),
                    Publisher = message.Label,
                    Payload = messageBody,
                    MetaData = properties,
                    ReceivedDateTime = receivedTime,
                    CreatedDate = DateTime.Now,
                };
                _logger.LogDebug($"Writing to the message repository messageid: {LoggerHelper.SanitizeValue(message.MessageId)}");
                MessagingRepository.Add(messageLog);
                _logger.LogDebug($"Writing to the message repository messageid: {LoggerHelper.SanitizeValue(message.MessageId)} completed");
            }
            catch (Exception e)
            {
                _logger.LogWarning(MessagingLoggingEvent.SubscriptionLogEntryFailed, e, $"Reading/Writing to subscription failed to primary. {LoggerHelper.SanitizeValue(message.SystemProperties.DeliveryCount)}, Subscription Name: {LoggerHelper.SanitizeValue(EntityName)}");

                // For retry
                if (message.SystemProperties.DeliveryCount == DeliveryCount)
                {
                    try
                    {
                        _logger.LogWarning(MessagingLoggingEvent.SubscriptionLogEntryFailed, "Reading/Writing to the subscription Error log.");
                        JsonSerializer.Deserialize<AzureMessageData>(messageBody);
                        var messageLog = new FailedSubscriptionMessageLog
                        {
                            SubscriptionName = EntityName,
                            MessageGuid = Guid.Parse(message.MessageId),
                            TransactionId = message.CorrelationId ?? Guid.Empty.ToString(),
                            Publisher = message.Label,
                            ServiceBusEntityName = AzureClientEntity.Path.Substring(0, AzureClientEntity.Path.IndexOf("/", StringComparison.Ordinal)),
                            Payload = messageBody,
                            MetaData = properties,

                            // we don't have received time, so setting this so that this date can be different than createDate
                            FailedDateTime = receivedTime,
                            Status = "FAILED",
                            CreatedDate = DateTime.Now,
                            ErrorMessage = e.Message + e.StackTrace,
                        };
                        MessagingRepository.Add(messageLog);

                        // Any failure will go back to the queue
                        await AzureClientEntity.CompleteAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
                    }
                    catch (Exception e1)
                    {
                        _logger.LogError(MessagingLoggingEvent.SubscriptionLogEntryFailed, e1, "Writing to the failed to the db log. This will go to deadletter queue");
                        await AbandonLock(message).ConfigureAwait(false); // this will not throw any error back.
                    }
                }
                else
                {
                    // if no delivery count reached abandon the message so that it will retry.
                    await AbandonLock(message).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Abandons a Microsoft.Azure.ServiceBus.Message using a lock token.
        /// This will make the message available again for processing.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <returns>Completed Task.</returns>
        private async Task<int> AbandonLock(Message message)
        {
            try
            {
                await AzureClientEntity.AbandonAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(MessagingLoggingEvent.SubscriptionLogEntryFailed, e, "Message abandon failed");
            }

            return 0;
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types
