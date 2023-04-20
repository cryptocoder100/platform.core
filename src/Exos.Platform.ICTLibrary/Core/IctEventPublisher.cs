#pragma warning disable CA1031 // Do not catch general exception types

namespace Exos.Platform.ICTLibrary.Core
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Helpers;
    using Exos.Platform.ICTLibrary.Core.Exception;
    using Exos.Platform.ICTLibrary.Core.Model;
    using Exos.Platform.ICTLibrary.Repository;
    using Exos.Platform.ICTLibrary.Repository.Model;
    using Exos.Platform.Messaging.Core;
    using Exos.Platform.Messaging.Helper;
    using Microsoft.Extensions.Logging;

    /// <inheritdoc/>
    public class IctEventPublisher : IIctEventPublisher
    {
        private readonly IExosMessaging _exosMessaging;
        private readonly ILogger<IctEventPublisher> _logger;
        private readonly IIctRepository _ictRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="IctEventPublisher"/> class.
        /// </summary>
        /// <param name="exosMessaging">The exosMessaging<see cref="IExosMessaging"/>.</param>
        /// <param name="ictRepository">The ictRepository<see cref="IIctRepository"/>.</param>
        /// <param name="logger">The logger<see cref="ILogger{IctEventPublisher}"/>.</param>
        public IctEventPublisher(IExosMessaging exosMessaging, IIctRepository ictRepository, ILogger<IctEventPublisher> logger)
        {
            _exosMessaging = exosMessaging ?? throw new ArgumentNullException(nameof(exosMessaging));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ictRepository = ictRepository ?? throw new ArgumentNullException(nameof(ictRepository));
        }

        /// <inheritdoc/>
        public async Task<bool> PublishEvent(IctEventMessage ictEventMessage, bool isTrackingInDbEnabled = false)
        {
            if (ictEventMessage == null)
            {
                throw new ArgumentNullException(nameof(ictEventMessage));
            }

            // Check entity event configuration, find the topic name
            // Publish the message
            // Insert into event tracking
            // _logger.LogDebug($"Publishing the event {LoggerHelper.SanitizeValue(ictEventMessage)}");
            var eventEntity = await _ictRepository.GetEventEntityTopic(ictEventMessage.EntityName, ictEventMessage.EventName, ictEventMessage.PublisherName).ConfigureAwait(false);
            try
            {
                if (eventEntity == null)
                {
                    _logger.LogError($"Event Entity could not be found for Entity: {LoggerHelper.SanitizeValue(ictEventMessage.EntityName)} , EventName: {LoggerHelper.SanitizeValue(ictEventMessage.EventName)} ,PublisherName: {LoggerHelper.SanitizeValue(ictEventMessage.PublisherName)}");
                    throw new IctException("Event Entity could not be found for Entity  {ictEventEvent.EntityName} , EventName: {ictEventMessage.EventName} ,PublisherName: {ictEventMessage.PublisherName}");
                }
            }
            catch (System.Exception e)
            {
                // If entity is not defined, log the error and move on. Application should not be stuck just because one entity is not configured.
                _logger.LogError(e, $"Event Entity could not be found for Entity  {LoggerHelper.SanitizeValue(ictEventMessage.EntityName)} , EventName: {LoggerHelper.SanitizeValue(ictEventMessage.EventName)} ,PublisherName: {LoggerHelper.SanitizeValue(ictEventMessage.PublisherName)}");
                return true;
            }

            var publishingAppName = eventEntity.Publisher.EventPublisherName;
            string topicName = eventEntity.TopicName;
            // _logger.LogDebug($"Publishing Message to the Owner {publishingAppName} and topic {topicName}");
            var message = new ExosMessage
            {
                Configuration = new MessageConfig { EntityName = topicName, EntityOwner = publishingAppName },
                MessageData = new MessageData
                {
                    PublisherMessageUniqueId = ictEventMessage.TrackingId,
                    Payload = JsonSerializer.Serialize(ictEventMessage),
                    AdditionalMetaData = new List<MessageMetaData>
                    {
                        new MessageMetaData { DataFieldName = "EventName", DataFieldValue = ictEventMessage.EventName, },
                        new MessageMetaData { DataFieldName = "EntityName", DataFieldValue = ictEventMessage.EntityName, },
                        new MessageMetaData { DataFieldName = "Priority", DataFieldValue = ictEventMessage.Priority.ToString(CultureInfo.InvariantCulture), },
                    },
                },
            };

            // Added event publisher to meta data so that same topic can be used and have subscription filter to run different workflows.
            if (eventEntity.Publisher != null)
            {
                message.MessageData.AdditionalMetaData.Add(new MessageMetaData
                {
                    DataFieldName = "EventPublisherName",
                    DataFieldValue = eventEntity.Publisher.EventPublisherName,
                });
                message.MessageData.AdditionalMetaData.Add(new MessageMetaData
                {
                    DataFieldName = "EventPublisherId",
                    DataFieldValue = eventEntity.Publisher.EventPublisherId.ToString(CultureInfo.InvariantCulture),
                });
            }

            if (ictEventMessage.AdditionalMessageHeaderData != null)
            {
                foreach (var a in ictEventMessage.AdditionalMessageHeaderData)
                {
                    message.MessageData.AdditionalMetaData.Add(new MessageMetaData
                    {
                        DataFieldName = a.Key,
                        DataFieldValue = a.Value,
                    });
                }
            }

            if (IsPublishingEnabled(message, eventEntity))
            {
                var result = await PublishEventImpl(message).ConfigureAwait(false);
                // _logger.LogDebug($"Publish Message result {result}");

                if (isTrackingInDbEnabled)
                {
                    var evt = new EventTracking
                    {
                        ApplicationName = publishingAppName,
                        EntityName = ictEventMessage.EntityName,
                        TrackingId = ictEventMessage.TrackingId ?? Guid.NewGuid().ToString(),
                        CreatedDate = DateTime.UtcNow,
                        LastUpdatedDate = DateTime.UtcNow,
                        TopicName = topicName,
                    };

                    try
                    {
                        await _ictRepository.AddEventTracking(evt).ConfigureAwait(false);
                    }
                    catch (System.Exception e)
                    {
                        _logger.LogError(e, "Tracking failed. Event completed");
                    }
                }

                // _logger.LogDebug("Publish Event completed");
            }
            else
            {
                _logger.LogDebug($"Event did not publish as  : {LoggerHelper.SanitizeValue(ictEventMessage.EntityName)} , EventName: {LoggerHelper.SanitizeValue(ictEventMessage.EventName)} ,PublisherName: {LoggerHelper.SanitizeValue(ictEventMessage.PublisherName)} has publishing flag disabled in EventEntityTopic");
            }

            return true;
        }

        /// <inheritdoc/>
        public async Task<IList<IctEventMessage>> PublishEvents(List<IctEventMessage> ictEventMessages, bool isTrackingInDbEnabled = false)
        {
            if (ictEventMessages == null || !ictEventMessages.Any())
            {
                return ictEventMessages;
            }

            // Split batch by EntityName, EventName, PublisherName we can have multiple entities in the collection
            var batchGroupList = ictEventMessages.Select(data => data).GroupBy(g => new { g.EntityName, g.EventName, g.PublisherName }).ToList();
            List<ExosMessage> exosMessages = new List<ExosMessage>();
            List<EventTracking> eventTrackings = new List<EventTracking>();
            List<ExosMessage> failedMessages = new List<ExosMessage>();
            List<IctEventMessage> failedIctMessages = new List<IctEventMessage>();
            foreach (var batchGroup in batchGroupList)
            {
                // Check entity event configuration, find the topic name by
                var eventEntityTopic = await _ictRepository.GetEventEntityTopic(batchGroup.Key.EntityName, batchGroup.Key.EventName, batchGroup.Key.PublisherName).ConfigureAwait(false);

                if (eventEntityTopic != null)
                {
                    foreach (var ictEventMessage in batchGroup)
                    {
                        exosMessages.Add(CreateExosMessage(eventEntityTopic, ictEventMessage));
                        eventTrackings.Add(new EventTracking
                        {
                            ApplicationName = eventEntityTopic.Publisher.EventPublisherName,
                            EntityName = ictEventMessage.EntityName,
                            TrackingId = ictEventMessage.TrackingId ?? Guid.NewGuid().ToString(),
                            TopicName = eventEntityTopic.TopicName,
                        });
                    }
                }
                else
                {
                    foreach (var ictEventMessage in batchGroup)
                    {
                        failedMessages.Add(new ExosMessage
                        {
                            MessageData = new MessageData
                            {
                                PublisherMessageUniqueId = ictEventMessage.TrackingId,
                                Payload = JsonSerializer.Serialize(ictEventMessage),
                                AdditionalMetaData = new List<MessageMetaData>
                                {
                                    new MessageMetaData { DataFieldName = "EventName", DataFieldValue = ictEventMessage.EventName, },
                                    new MessageMetaData { DataFieldName = "EntityName", DataFieldValue = ictEventMessage.EntityName, },
                                    new MessageMetaData { DataFieldName = "Priority", DataFieldValue = ictEventMessage.Priority.ToString(CultureInfo.InvariantCulture), },
                                },
                            },
                        });
                    }

                    _logger.LogError($"Event Entity could not be found for Entity: {LoggerHelper.SanitizeValue(batchGroup.Key.EntityName)} , EventName: {LoggerHelper.SanitizeValue(batchGroup.Key.EventName)} ,PublisherName: {LoggerHelper.SanitizeValue(batchGroup.Key.PublisherName)}");
                }
            }

            // Publish the events
            IList<ExosMessage> failedPublishedMessages = await _exosMessaging.PublishMessages(exosMessages).ConfigureAwait(false);
            failedMessages.AddRange(failedPublishedMessages);

            if (failedMessages.Any())
            {
                // Remove failed messages from original list, we need to add tracking only for succesful messages
                eventTrackings.RemoveAll(e => failedMessages.Any(f => f.MessageData.PublisherMessageUniqueId == e.TrackingId));

                // Return failed messages, remove messages that doesn't fail.
                var result = ictEventMessages.FindAll(ict => failedMessages.Any(f => f.MessageData.PublisherMessageUniqueId == ict.TrackingId));

                failedIctMessages.AddRange(result);
            }

            // Add event Tracking
            if (eventTrackings.Any() && isTrackingInDbEnabled)
            {
                var eventDateTimeUtc = DateTime.UtcNow;
                foreach (var eventTracking in eventTrackings)
                {
                    eventTracking.CreatedDate = eventDateTimeUtc;
                    eventTracking.LastUpdatedDate = eventDateTimeUtc;
                    try
                    {
                        await _ictRepository.AddEventTracking(eventTracking).ConfigureAwait(false);
                    }
                    catch (System.Exception e)
                    {
                        _logger.LogError(e, "Tracking failed. Event completed");
                    }
                }
            }

            // Return only the failed messages, should be empty if all of them succeed.
            return failedIctMessages;
        }

        private static ExosMessage CreateExosMessage(EventEntityTopic eventEntityTopic, IctEventMessage ictEventMessage)
        {
            var exosMessage = new ExosMessage
            {
                Configuration = new MessageConfig { EntityName = eventEntityTopic.TopicName, EntityOwner = eventEntityTopic.Publisher.EventPublisherName },
                MessageData = new MessageData
                {
                    PublisherMessageUniqueId = ictEventMessage.TrackingId,
                    Payload = JsonSerializer.Serialize(ictEventMessage),
                    AdditionalMetaData = new List<MessageMetaData>
                    {
                        new MessageMetaData { DataFieldName = "EventName", DataFieldValue = ictEventMessage.EventName, },
                        new MessageMetaData { DataFieldName = "EntityName", DataFieldValue = ictEventMessage.EntityName, },
                        new MessageMetaData { DataFieldName = "Priority", DataFieldValue = ictEventMessage.Priority.ToString(CultureInfo.InvariantCulture), },
                    },
                },
            };

            // Added event publisher to meta data so that same topic can be used and have subscription filter to run different workflows.
            if (eventEntityTopic.Publisher != null)
            {
                exosMessage.MessageData.AdditionalMetaData.Add(new MessageMetaData
                {
                    DataFieldName = "EventPublisherName",
                    DataFieldValue = eventEntityTopic.Publisher.EventPublisherName,
                });
                exosMessage.MessageData.AdditionalMetaData.Add(new MessageMetaData
                {
                    DataFieldName = "EventPublisherId",
                    DataFieldValue = eventEntityTopic.Publisher.EventPublisherId.ToString(CultureInfo.InvariantCulture),
                });
            }

            if (ictEventMessage.AdditionalMessageHeaderData != null)
            {
                foreach (var a in ictEventMessage.AdditionalMessageHeaderData)
                {
                    exosMessage.MessageData.AdditionalMetaData.Add(new MessageMetaData
                    {
                        DataFieldName = a.Key,
                        DataFieldValue = a.Value,
                    });
                }
            }

            return exosMessage;
        }

        private static bool IsPublishingEnabled(ExosMessage message, EventEntityTopic eventEntity)
        {
            var targetMessagingPlatform = GetTargetMessagingPlatform(message);

            switch (targetMessagingPlatform)
            {
                case TargetMessagingPlatform.EventHub:
                    {
                        return eventEntity.IsPublishToEventHubActive;
                    }

                case TargetMessagingPlatform.ServiceBus:
                default:
                    {
                        return eventEntity.IsPublishToServiceBusActive;
                    }
            }
        }

        /// <summary>
        /// "TargetMessagingPlatform", "ServiceBus"
        ///  "TargetMessagingPlatform", "EventHub".
        /// </summary>
        /// <param name="ictEventEvent">ictEventEvent.</param>
        /// <returns>Target Messaging Platform.</returns>
        private static TargetMessagingPlatform GetTargetMessagingPlatform(ExosMessage ictEventEvent)
        {
            // Default to service bus.
            TargetMessagingPlatform targetMessagingPlatform = TargetMessagingPlatform.ServiceBus;
            var target = ictEventEvent.MessageData?.AdditionalMetaData.Where(i => i.DataFieldName == Constants.TargetMessagingPlatform).FirstOrDefault();

            if (target != null)
            {
                if (Enum.TryParse<TargetMessagingPlatform>(target.DataFieldValue, out targetMessagingPlatform))
                {
                    return targetMessagingPlatform;
                }
            }

            return targetMessagingPlatform;
        }

        /// <summary>
        /// New changes, letting the caller decide the platform, we could have determined that in this library
        /// also but we will run into commital across Service Bus, Event Hubs etc.
        /// Same Message needs commited to service bus and event hub.
        /// Letting the caller decide and control re-tries etc, it also separates the concerns if we try to
        /// commit into Service Bus and Event Hub from the same call.
        /// </summary>
        /// <param name="message">message.</param>
        /// <returns>success code.</returns>
        private async Task<int> PublishEventImpl(ExosMessage message)
        {
            var targetMessagingPlatform = GetTargetMessagingPlatform(message);
            int result;
            switch (targetMessagingPlatform)
            {
                case TargetMessagingPlatform.EventHub:
                    {
                        result = await _exosMessaging.PublishMessageToEventHub(message).ConfigureAwait(false);
                        break;
                    }

                case TargetMessagingPlatform.ServiceBus:
                default:
                    {
                        result = await _exosMessaging.PublishMessageToTopic(message).ConfigureAwait(false);
                        break;
                    }
            }

            if (result != 9999)
            {
                throw new IctException("Could not publish message to queue");
            }

            return result;
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types
