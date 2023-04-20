#pragma warning disable CA1715 // Identifiers should have correct prefix
#pragma warning disable CA1031 // Do not catch general exception types
namespace Exos.Platform.Persistence.EventPoller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Transactions;
    using Exos.Platform.ICTLibrary.Core;
    using Exos.Platform.ICTLibrary.Core.Model;
    using Exos.Platform.Persistence.EventTracking;
    using Exos.Platform.TenancyHelper.MultiTenancy;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <inheritdoc/>
    public class ICTEventPollerServiceBusService<T, TCP> : ICTEventPollerBaseService<T, TCP>, IICTEventPollerCheckPointServiceBusService
        where T : EventTrackingEntity, new()
        where TCP : EventPublishCheckPointEntity, new()
    {
        private static PollerProcess _pollerProcess = PollerProcess.ServiceBus;
        private readonly ILogger<ICTEventPollerServiceBusService<T, TCP>> _logger;
        private readonly EventPollerServiceSettings _eventPollerServiceSettings;
        private readonly IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="ICTEventPollerServiceBusService{T, TCP}"/> class.
        /// </summary>
        /// <param name="services"><see cref="IServiceProvider"/>.</param>
        /// <param name="options"><see cref="EventPollerServiceSettings"/>.</param>
        /// <param name="logger">ICTEventPollerServiceBusService <see cref="ILogger"/>.</param>
        /// <param name="loggerBase">ICTEventPollerEventHubService <see cref="ILogger"/>.</param>
        public ICTEventPollerServiceBusService(IServiceProvider services, IOptions<EventPollerServiceSettings> options, ILogger<ICTEventPollerServiceBusService<T, TCP>> logger, ILogger<ICTEventPollerEventHubService<T, TCP>> loggerBase)
            : base(services, options, loggerBase)
        {
            _services = services;
            _eventPollerServiceSettings = options?.Value;
            _logger = logger;
        }

        /// <inheritdoc/>
        public override PollerProcess GetPollerProcess()
        {
            return _pollerProcess;
        }

        /// <inheritdoc/>
        public override List<KeyValuePair<string, string>> GetAdditionalMetaData()
        {
            var lst = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(Constants.TargetMessagingPlatform, TargetMessagingPlatform.ServiceBus.ToString())
            };
            return lst;
        }

        /// <summary>
        /// Set events to ICT.
        /// </summary>
        /// <param name="eventTrackingEvents">List of events to send.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task<List<T>> SendICTEvents(List<T> eventTrackingEvents)
        {
            if (eventTrackingEvents is null)
            {
                throw new ArgumentNullException(nameof(eventTrackingEvents));
            }

            if (!_eventPollerServiceSettings.SendEventsInBatch)
            {
                return await base.SendICTEvents(eventTrackingEvents);
            }
            else
            {
                _logger.LogDebug("Send Events to ICT in batch");
                List<IctEventMessage> ictEventMessages = new List<IctEventMessage>();
                using (var scope = _services.CreateScope())
                {
                    var ictEventPublisher = scope.ServiceProvider.GetRequiredService<IIctEventPublisher>();
                    int numberOfMessages = 0;
                    foreach (T eventTrackingEntity in eventTrackingEvents)
                    {
                        numberOfMessages++;
                        List<KeyValuePair<string, string>> additionalMessageHeaderData = new List<KeyValuePair<string, string>>();
                        if (eventTrackingEntity.Metadata != null)
                        {
                            _logger.LogDebug("Meta data of published event is " + eventTrackingEntity.Metadata);
                            var messageMetadata = JsonSerializer.Deserialize<Dictionary<string, string>>(eventTrackingEntity.Metadata);
                            foreach (var kv in messageMetadata)
                            {
                                additionalMessageHeaderData.Add(new KeyValuePair<string, string>(kv.Key, kv.Value));
                            }
                        }

                        additionalMessageHeaderData.AddRange(GetAdditionalMetaData());

                        var eventMessage = new IctEventMessage
                        {
                            UserContext = JsonSerializer.Deserialize<UserContext>(eventTrackingEntity.UserContext),
                            TrackingId = eventTrackingEntity.TrackingId,
                            Priority = eventTrackingEntity.Priority,
                            EventName = eventTrackingEntity.EventName,
                            EntityName = eventTrackingEntity.EntityName,
                            PublisherName = eventTrackingEntity.PublisherName,
                            PublisherId = eventTrackingEntity.PublisherId,
                            Payload = eventTrackingEntity.Payload,
                            AdditionalMessageHeaderData = additionalMessageHeaderData,
                        };
                        ictEventMessages.Add(eventMessage);
                    }

                    // Publish the events in batch events are ordered by Entity Name to create multiple batchs for each entity
                    IList<IctEventMessage> failedEvents = await ictEventPublisher.PublishEvents(ictEventMessages, _eventPollerServiceSettings.IsIctEventTrackingEnabled);

                    if (failedEvents.Any())
                    {
                        // Return failed messages
                        var failedEventTrackingEvents = eventTrackingEvents.FindAll(ict => failedEvents.Any(f => f.TrackingId == ict.TrackingId));
                        return failedEventTrackingEvents;
                    }
                    else
                    {
                        return new List<T>();
                    }
                }
            }
        }
    }
}
#pragma warning restore CA1715 // Identifiers should have correct prefix
#pragma warning restore CA1031 // Do not catch general exception types