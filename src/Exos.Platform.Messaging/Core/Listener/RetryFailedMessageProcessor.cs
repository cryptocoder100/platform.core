#pragma warning disable CA1031 // Do not catch general exception types
namespace Exos.Platform.Messaging.Core.Listener
{
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Exos.Platform.Messaging.Helper;
    using Exos.Platform.Messaging.Repository.Model;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <inheritdoc/>
    public class RetryFailedMessageProcessor : MessageProcessor
    {
        private readonly MessageSection _messageSection;
        private readonly ILogger<RetryFailedMessageProcessor> _logger;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryFailedMessageProcessor"/> class.
        /// </summary>
        /// <param name="serviceProvider">serviceProvider.</param>
        public RetryFailedMessageProcessor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _messageSection = serviceProvider.GetService<IOptions<MessageSection>>().Value;
            _logger = serviceProvider.GetService<ILogger<RetryFailedMessageProcessor>>();
        }

        /// <inheritdoc/>
        public override async Task<bool> Execute(string messageUtfText, MessageProperty messageProperty)
        {
            using var scope = _serviceProvider.CreateScope();
            long failedSubscriptionMessageLogId = 0;
            string errorMessage = string.Empty;
            IFailedMessageService failedMessageService = scope.ServiceProvider.GetService<IFailedMessageService>();

            if (failedMessageService == null)
            {
                errorMessage = $"IFailedMessageService is null.";
                _logger.LogError(errorMessage);
                return false;
            }

            try
            {
                AzureMessageData inputRequest = JsonSerializer.Deserialize<AzureMessageData>(messageUtfText);
                if (inputRequest.Message == null)
                {
                    errorMessage = $"Input messageUtfText de-serialization to AzureMessageData failed";
                    _logger.LogError(errorMessage);
                    return false;
                }

                FailedSubscriptionMessageLog failedSubscription = JsonSerializer.Deserialize<FailedSubscriptionMessageLog>(inputRequest.Message.Payload);
                if (failedSubscription.Payload == null)
                {
                    errorMessage = $"Input messageUtfText payload de-serialization to FailedSubscriptionMessageLog failed.";
                    _logger.LogError(errorMessage);
                    return false;
                }

                failedSubscriptionMessageLogId = failedSubscription.FailedSubscriptionMessageLogId;
                var matchedFailedSubscriptions = await failedMessageService.GetFailedMessagesByIds(new long[] { failedSubscriptionMessageLogId }).ConfigureAwait(false);
                if (matchedFailedSubscriptions == null || matchedFailedSubscriptions.Count() != 1)
                {
                    // There should be one and only one object in Database
                    errorMessage = $"FailedSubscriptionMessageLog not found from the input message. FailedSubscriptionMessageLogId: {failedSubscription.FailedSubscriptionMessageLogId}";
                    _logger.LogError(errorMessage);
                    return false;
                }

                if (matchedFailedSubscriptions.ElementAt(0).Status == ExosMessagingConstant.AzureMessagePublishStatusSucceeded)
                {
                    // Ignore the retry if the message already succeeded and return true
                    _logger.LogWarning($"Ignoring the Message: {failedSubscriptionMessageLogId} retry as it was already succeeded.");
                    return true;
                }

                MessageListenerConfig messageListenerConfig = _messageSection.Listeners.Where(
                    listener => !listener.DisabledFlg && listener.SubscriptionName == failedSubscription.SubscriptionName
                    && listener.EntityName == failedSubscription.ServiceBusEntityName).FirstOrDefault();

                if (messageListenerConfig == null)
                {
                    // Ignore the retry if the listener not found in the configuration settings and return true
                    errorMessage = $"Ignoring this retry as listener processor not found from configuration for TopicName: {failedSubscription.ServiceBusEntityName} and SubscriptionName: {failedSubscription.SubscriptionName}";
                    _logger.LogWarning(errorMessage);
                    return true;
                }

                Type processorType = Type.GetType(messageListenerConfig.Processor, true, false);

                if (Activator.CreateInstance(processorType, _serviceProvider) is MessageProcessor processor)
                {
                    var retryResult = await processor.Execute(failedSubscription.Payload, messageProperty).ConfigureAwait(false);
                    string retryStatusMessage = retryResult
                        ? ExosMessagingConstant.AzureMessagePublishStatusSucceeded
                        : ExosMessagingConstant.AzureMessagePublishStatusFailed;

                    await failedMessageService.UpdateFailedMessageStatus(failedSubscriptionMessageLogId, retryStatusMessage).ConfigureAwait(false);
                    return true;
                }
            }
            catch (Exception e)
            {
                try
                {
                    if (failedSubscriptionMessageLogId > 0)
                    {
                        await failedMessageService.UpdateFailedMessageErrorMessage(failedSubscriptionMessageLogId, e.ToString()).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception updating Failed Error Message");
                }
            }

            return true;
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types