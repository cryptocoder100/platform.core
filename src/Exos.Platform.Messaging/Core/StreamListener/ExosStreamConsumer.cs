#pragma warning disable CA2201 // Do not raise reserved exception types
#pragma warning disable CA1031 // Do not catch general exception types
namespace Exos.Platform.Messaging.Core.StreamListener
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.Messaging.Core.Listener;
    using Exos.Platform.Messaging.Helper;
    using Exos.Platform.Messaging.Repository;
    using Exos.Platform.Messaging.Repository.Model;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.EventHubs.Processor;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    /// <summary>
    /// this processor is hit when hubs needs to pass the messages to consumer.
    /// </summary>
    public class ExosStreamConsumer : IEventProcessor, IExosStreamConsumer
    {
        /// <summary>
        /// PartitionContext.
        /// </summary>
        public const string PartitionContext = "PartitionContext";

        /// <summary>
        /// Properties.
        /// </summary>
        public const string Properties = "Properties";

        private IServiceProvider _serviceProvider;
        private Stopwatch _checkpointStopWatch;
        private ILogger<ExosStreamConsumer> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExosStreamConsumer"/> class.
        /// </summary>
        /// <param name="serviceProvider">serviceProvider.</param>
        public ExosStreamConsumer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<ExosStreamConsumer>>();
        }

        /// <summary>
        /// Gets or sets ExosStreamProcessor.
        /// </summary>
        public ExosStreamProcessor ExosStreamProcessor { get; set; }

        /// <summary>
        /// Gets or sets MessageEntity.
        /// </summary>
        public MessageEntity MessageEntity { get; set; }

        /// <summary>
        /// Gets or sets MessagingRepository.
        /// </summary>
        public IMessagingRepository MessagingRepository { get; set; }

        /// <summary>
        /// CloseAsync.
        /// </summary>
        /// <param name="context">context.</param>
        /// <param name="reason">reason.</param>
        /// <returns>Task.</returns>
        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Console.WriteLine($"Processor Shutting Down. Partition '{context.PartitionId}', Reason: '{reason}'.");
            _logger.LogWarning(new Exception(reason.ToString()), $"Processor Shutting Down. Partition '{LoggerHelper.SanitizeValue(context.PartitionId)}', Reason: '{LoggerHelper.SanitizeValue(reason)}'.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// OpenAsync.
        /// </summary>
        /// <param name="context">context.</param>
        /// <returns>Task.</returns>
        public Task OpenAsync(PartitionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _logger.LogDebug("SimpleEventProcessor initialized.  Partition: '{0}', Offset: '{1}'", LoggerHelper.SanitizeValue(context.Lease.PartitionId), LoggerHelper.SanitizeValue(context.Lease.Offset));
            Console.WriteLine($"Processor OpenAsync. Partition '{context.PartitionId}'");

            _checkpointStopWatch = new Stopwatch();
            _checkpointStopWatch.Start();
            return Task.CompletedTask;
        }

        /// <summary>
        /// ProcessErrorAsync.
        /// </summary>
        /// <param name="context">context.</param>
        /// <param name="error">error.</param>
        /// <returns>Task.</returns>
        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            // Treating the following exceptions as info, these are raised by framework during load balancing.
            if (error is ReceiverDisconnectedException || error is LeaseLostException)
            {
                _logger.LogDebug(error, $"Info on Partition: {LoggerHelper.SanitizeValue(context.PartitionId)}, Info: {LoggerHelper.SanitizeValue(error.Message)}");
            }
            else
            {
                _logger.LogError(error, $"Error on Partition: {LoggerHelper.SanitizeValue(context.PartitionId)}, Error: {LoggerHelper.SanitizeValue(error.Message)}");
                Console.WriteLine($"Error on Partition: {context.PartitionId}, Error: {error.Message}");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// ProcessEventsAsync.
        /// </summary>
        /// <param name="context">context.</param>
        /// <param name="messages">messages.</param>
        /// <returns>Task.</returns>
        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            List<ExosStream> messagesStream = null;
            // _logger.LogDebug($"Event processor Message received and starting to process. Partition details are {LoggerHelper.SanitizeValue(context)}");
            try
            {
                if (messages != null)
                {
                    // Convert
                    messagesStream = ConvertEventDataToExosFormat(messages, context);
                }

                if (messagesStream != null)
                {
                    // Process
                    var failedMessages = await ExosStreamProcessor.Execute(messagesStream).ConfigureAwait(false);

                    if (failedMessages != null && failedMessages.Count > 0)
                    {
                        LogFailedMessages(failedMessages);
                    }
                }

                // _logger.LogDebug("Message completing subscription");

                // Call checkpoint every 5 minutes, so that worker can resume processing from 5 minutes back if it restarts.
                if (_checkpointStopWatch.Elapsed > TimeSpan.FromMinutes(5))
                {
                    _logger.LogDebug($"Event processor Checking pointing at {DateTime.UtcNow}");
                    await context.CheckpointAsync().ConfigureAwait(false);
                    _checkpointStopWatch.Restart();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Reading/Writing to subscription failed to primary.Subscription Name: {MessageEntity.EntityName}, Partition id:{LoggerHelper.SanitizeValue(context.PartitionId)}");
                try
                {
                    if (messagesStream != null)
                    {
                        var failedMessages = ConvertToFailedMessages(messagesStream, e);

                        LogFailedMessages(failedMessages);
                    }
                }
                catch (Exception e1)
                {
                    _logger.LogError(e1, $"Reading/Writing to failed message DB failed.Subscription Name: {MessageEntity.EntityName}");
                }
            }
        }

        private static List<ExosFailedStream> ConvertToFailedMessages(List<ExosStream> failedMessages, Exception e)
        {
            List<ExosFailedStream> failedStream = null;

            if (failedMessages != null && failedMessages.Count > 0)
            {
                failedStream = new List<ExosFailedStream>();
                foreach (var filedMsg in failedMessages)
                {
                    failedStream.Add(new ExosFailedStream() { ExosStream = filedMsg, Exception = e });
                }
            }

            return failedStream;
        }

        private static List<ExosStream> ConvertEventDataToExosFormat(IEnumerable<EventData> messages, PartitionContext partitionContext)
        {
            Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();
            keyValuePairs.Add(PartitionContext, JsonConvert.SerializeObject(partitionContext));
            List<ExosStream> exosStreams = new List<ExosStream>();
            foreach (var eventData in messages)
            {
                if (keyValuePairs.ContainsKey(Properties))
                {
                    keyValuePairs.Remove(Properties);
                }

                keyValuePairs.Add(Properties, JsonConvert.SerializeObject(eventData.Properties));

                exosStreams.Add(new ExosStream()
                {
                    MessageUtfText = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count),
                    Property = new MessageProperty()
                    {
                        MetaDataProperties = JsonConvert.SerializeObject(keyValuePairs),
                        EnqueuedTimeUtc = eventData?.SystemProperties?.EnqueuedTimeUtc,
                        Offset = eventData?.SystemProperties?.Offset,
                        SequenceNumber = eventData?.SystemProperties?.SequenceNumber,
                    },
                });
            }

            return exosStreams;
        }

        private void LogFailedMessages(List<ExosFailedStream> failedStreamToLog)
        {
            if (failedStreamToLog != null && failedStreamToLog.Count > 0)
            {
                foreach (var exosFaileddMsg in failedStreamToLog)
                {
                    _logger.LogError(MessagingLoggingEvent.SubscriptionLogEntryFailed, "Reading/Writing to the subscription Error log.");
                    var azureMessage = JsonConvert.DeserializeObject<AzureMessageData>(exosFaileddMsg.ExosStream.MessageUtfText);
                    var messageLog = new FailedSubscriptionMessageLog
                    {
                        SubscriptionName = MessageEntity.EntityName,
                        MessageGuid = azureMessage.MessageGuid,
                        TransactionId = azureMessage.Message.PublisherMessageUniqueId ?? Guid.Empty.ToString(),
                        Publisher = MessageEntity.Owner,
                        ServiceBusEntityName = MessageEntity.EntityName,
                        Payload = exosFaileddMsg.ExosStream.MessageUtfText,
                        MetaData = exosFaileddMsg.ExosStream.Property.MetaDataProperties,
                        FailedDateTime = DateTime.Now,
                        Status = "FAILED",
                        CreatedDate = DateTime.Now,
                        ErrorMessage = exosFaileddMsg.Exception != null ? exosFaileddMsg.Exception.Message + exosFaileddMsg.Exception.StackTrace : "No error details provided.",
                    };
                    MessagingRepository.Add(messageLog);

                    // Any failure will go back to the queue
                    // _logger.LogDebug("Message completed successfully in Subscription");
                }
            }
        }
    }
}
#pragma warning restore CA2201 // Do not raise reserved exception types
#pragma warning restore CA1031 // Do not catch general exception types
