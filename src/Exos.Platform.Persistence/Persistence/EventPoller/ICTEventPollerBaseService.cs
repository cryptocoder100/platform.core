#pragma warning disable CA1715 // Identifiers should have correct prefix
#pragma warning disable CA1031 // Do not catch general exception types
namespace Exos.Platform.Persistence.EventPoller
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
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
    using Newtonsoft.Json;

    /// <summary>
    /// Base class for ICT Event Poller.
    /// </summary>
    /// <typeparam name="T"><see cref="EventTrackingEntity"/>.</typeparam>
    /// <typeparam name="TCP"><see cref="EventPublishCheckPointEntity"/>.</typeparam>
    public abstract class ICTEventPollerBaseService<T, TCP>
        where T : EventTrackingEntity, new()
        where TCP : EventPublishCheckPointEntity, new()
    {
        private readonly ILogger<ICTEventPollerEventHubService<T, TCP>> _logger;
        private readonly EventPollerServiceSettings _eventPollerServiceSettings;
        private readonly IServiceProvider _services;
        private EventPollerServiceStatus _status;

        /// <summary>
        /// Initializes a new instance of the <see cref="ICTEventPollerBaseService{T, CP}"/> class.
        /// </summary>
        /// <param name="services"><see cref="IServiceProvider"/>.</param>
        /// <param name="eventPollerServiceSettings"><see cref="EventPollerServiceSettings"/>.</param>
        /// <param name="logger"><see cref="ILogger"/>.</param>
        protected ICTEventPollerBaseService(IServiceProvider services, IOptions<EventPollerServiceSettings> eventPollerServiceSettings, ILogger<ICTEventPollerEventHubService<T, TCP>> logger)
        {
            _services = services;
            _eventPollerServiceSettings = eventPollerServiceSettings?.Value;
            _logger = logger;
            _status = new EventPollerServiceStatus
            {
                PollingExecutions = 0,
            };
        }

        /// <summary>
        /// Services Bus Or Event hub.
        /// </summary>
        /// <returns>Return PollerProcess instance.</returns>
        public abstract PollerProcess GetPollerProcess();

        /// <summary>
        /// Get Additional MetaData.
        /// </summary>
        /// <returns>Metadata.</returns>
        public abstract List<KeyValuePair<string, string>> GetAdditionalMetaData();

        /// <summary>
        /// Get Poller status.
        /// </summary>
        /// <returns>Poller status.</returns>
        public EventPollerServiceStatus ICTEventPollerServiceStatus()
        {
            return _status;
        }

        /// <summary>
        /// Execute poller.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync()
        {
            _status.EventPollerName = "ICT Scheduler Event Poller Service.";
            _status.State = EventPollerServiceState.Starting;

            if (_eventPollerServiceSettings == null || _eventPollerServiceSettings.IsPollingEnabled == false)
            {
                _logger.LogDebug("ICT Scheduler Event Poller Service; polling is not enabled, exiting the poller.");
                _status.State = EventPollerServiceState.Disabled;
                return;
            }

            // Running
            if (_status.PollingExecutions == 0)
            {
                _status.Started = DateTimeOffset.UtcNow;
            }

            _status.State = EventPollerServiceState.Running;
            _logger.LogDebug("Running ICT Scheduler Event Poller Service for " + GetPollerProcess().ToString());

            List<T> eventTrackingEvents = null;
            try
            {
                _status.PollingExecutions++;
                using (var scope = _services.CreateScope())
                {
                    using (var transactionScope = new TransactionScope(
                        TransactionScopeOption.Required,
                        new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                        TransactionScopeAsyncFlowOption.Enabled))
                    {
                        var eventTrackingRepository = scope.ServiceProvider.GetRequiredService<IEventSqlServerRepository<T>>();
                        eventTrackingEvents = await eventTrackingRepository.QueryEvents(_eventPollerServiceSettings.EventQuery, (byte)GetPollerProcess()).ConfigureAwait(false);
                        await SaveEventCheckPoints(eventTrackingEvents).ConfigureAwait(false);
                        transactionScope.Complete();
                    }

                    _logger.LogDebug($"ICT Scheduler Event Poller Service {eventTrackingEvents.Count} Events to Process.");
                    if (eventTrackingEvents?.Any() == true)
                    {
                        var failedEvents = await SendICTEvents(eventTrackingEvents).ConfigureAwait(false);
                        if (failedEvents.Any())
                        {
                            await RollBackEventCheckPoints(failedEvents).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ICT Scheduler Event Poller Service error {message}", ex.Message);
                await RollBackEventCheckPoints(eventTrackingEvents).ConfigureAwait(false);
                eventTrackingEvents.Clear();
            }

            if (_eventPollerServiceSettings.IsArchivalEnabled)
            {
                await ArchiveEvents(eventTrackingEvents).ConfigureAwait(false);
            }

            _status.LastExecution = DateTimeOffset.UtcNow;

            // Scheduled for next execution
            _status.State = EventPollerServiceState.Finished;
            _logger.LogDebug("Finished ICT Scheduler Event Poller Service.");
        }

        /// <summary>
        /// Delete old events.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task HardDeleteOldEvents()
        {
            if (!string.IsNullOrEmpty(_eventPollerServiceSettings.EventQueueRetentionQuery))
            {
                _logger.LogTrace($"ICT Scheduler Event Poller Service HardDeleteProcessedEvents.");
                using (var scope = _services.CreateScope())
                {
                    using (var transactionScope = new TransactionScope(
                        TransactionScopeOption.Required,
                        new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                        TransactionScopeAsyncFlowOption.Enabled))
                    {
                        _logger.LogDebug($"ICT Scheduler Event Poller Service, running following query to hard delete the old events." + _eventPollerServiceSettings.EventQueueRetentionQuery);
                        var eventTrackingRepository = scope.ServiceProvider.GetRequiredService<IEventSqlServerRepository<T>>();
                        var eventCheckpointRepository = scope.ServiceProvider.GetRequiredService<IEventPublishCheckPointSqlRepository<TCP>>();

                        var eventIdsToBeDeleted = await eventCheckpointRepository.GetEventIdsToBeDeleted(_eventPollerServiceSettings.EventsRetentionQuery).ConfigureAwait(false);

                        if (eventIdsToBeDeleted?.Count > 0)
                        {
                            var eventIds = eventIdsToBeDeleted.ToArray();

                            await HardDeleteEventCheckpoints(eventIds).ConfigureAwait(false);
                            await HardDeleteEvents(eventIds).ConfigureAwait(false);
                        }

                        _logger.LogDebug($"ICT Scheduler Event Poller Service hard deleted {eventIdsToBeDeleted?.Count} events.");
                        transactionScope.Complete();
                    }
                }
            }
            else
            {
                _logger.LogError($"ICT Scheduler Event Poller Service HardDeleteOldEvents Delete Query is not configured.");
            }
        }

        /// <summary>
        /// Set events to ICT.
        /// </summary>
        /// <param name="eventTrackingEvents">List of events to send.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public virtual async Task<List<T>> SendICTEvents(List<T> eventTrackingEvents)
        {
            if (eventTrackingEvents is null)
            {
                throw new ArgumentNullException(nameof(eventTrackingEvents));
            }

            List<T> failedEvents = new List<T>();
            using (var scope = _services.CreateScope())
            {
                var ictEventPublisher = scope.ServiceProvider.GetRequiredService<IIctEventPublisher>();
                int numberOfMessages = 0;
                int failedMessages = 0;
                foreach (T eventTrackingEntity in eventTrackingEvents)
                {
                    try
                    {
                        numberOfMessages++;
                        List<KeyValuePair<string, string>> additionalMessageHeaderData = new List<KeyValuePair<string, string>>();
                        if (eventTrackingEntity.Metadata != null)
                        {
                            _logger.LogDebug("meta data of published event is " + eventTrackingEntity.Metadata);
                            var messageMetadata = JsonConvert.DeserializeObject<Dictionary<string, string>>(eventTrackingEntity.Metadata);
                            foreach (var kv in messageMetadata)
                            {
                                additionalMessageHeaderData.Add(new KeyValuePair<string, string>(kv.Key, kv.Value));
                            }
                        }

                        additionalMessageHeaderData.AddRange(GetAdditionalMetaData());

                        var eventMessage = new IctEventMessage
                        {
                            UserContext = JsonConvert.DeserializeObject<UserContext>(eventTrackingEntity.UserContext),
                            TrackingId = eventTrackingEntity.TrackingId,
                            Priority = eventTrackingEntity.Priority,
                            EventName = eventTrackingEntity.EventName,
                            EntityName = eventTrackingEntity.EntityName,
                            PublisherName = eventTrackingEntity.PublisherName,
                            PublisherId = eventTrackingEntity.PublisherId,
                            Payload = eventTrackingEntity.Payload,
                            AdditionalMessageHeaderData = additionalMessageHeaderData,
                        };

                        _logger.LogDebug($"ICT Scheduler Event Poller Service Sending ICT message:{numberOfMessages} - {eventMessage}.");
                        await ictEventPublisher.PublishEvent(eventMessage, _eventPollerServiceSettings.IsIctEventTrackingEnabled).ConfigureAwait(false);
                        _logger.LogDebug($"ICT Scheduler Event Poller Service Message {numberOfMessages} Sent to ICT.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"ICT Scheduler Event Poller Service Error Sending to ICT Message: {numberOfMessages} - Event Id =  {eventTrackingEntity.EventId} ", ex.Message);
                    }
                }

                if (failedEvents.Any())
                {
                    // Remove failed messages from original list, we need to archive only succesful messages
                    eventTrackingEvents.RemoveAll(i => failedEvents.Contains(i));
                }

                _logger.LogDebug($"ICT Scheduler Event Poller Service Messages Sent to ICT: {numberOfMessages} - Failed Messages {failedMessages}.");
            }

            return failedEvents;
        }

        /// <summary>
        /// Archive Events.
        /// </summary>
        /// <param name="eventTrackingEvents">Events to update.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task ArchiveEvents(List<T> eventTrackingEvents)
        {
            if (eventTrackingEvents?.Any() == true)
            {
                using var scope = _services.CreateScope();
                using var transactionScope = new TransactionScope(
                    TransactionScopeOption.Required,
                    new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                    TransactionScopeAsyncFlowOption.Enabled);
                _logger.LogDebug($"ICT Scheduler Event Poller Service Archiving {eventTrackingEvents.Count} Events from Event Table.");
                var eventArchiveRepository = scope.ServiceProvider.GetRequiredService<IEventArchivalSqlServerRepository<T>>();
                await eventArchiveRepository.ArchiveEvents(eventTrackingEvents).ConfigureAwait(false);
                _logger.LogDebug($"ICT Scheduler Event Poller Service Archived {eventTrackingEvents.Count} Events from Event Table.");
                transactionScope.Complete();
            }
        }

        /// <summary>
        /// Save check point events.
        /// </summary>
        /// <param name="eventTrackingEvents">List of Event Tracking Events.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task SaveEventCheckPoints(List<T> eventTrackingEvents)
        {
            if (eventTrackingEvents?.Any() == true)
            {
                using (var scope = _services.CreateScope())
                {
                    var eventCheckPointRepository = scope.ServiceProvider.GetRequiredService<IEventPublishCheckPointSqlRepository<TCP>>();

                    List<TCP> checkPoints = new List<TCP>();
                    checkPoints = eventTrackingEvents.Select(eventToProcess =>
                    {
                        return new TCP()
                        {
                            EventId = eventToProcess.EventId,
                            IsActive = true,
                            ProcessId = (byte)GetPollerProcess(),
                            CreatedBy = "eventpoller",
                            CreatedDate = DateTime.UtcNow,
                            LastUpdatedDate = null
                        };
                    }).ToList();

                    await eventCheckPointRepository.CreateEventCheckPoints(checkPoints).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Hard delete alert!!
        /// Hard delete is by design in case of exceptions during Message publish else it can create run away
        /// inserts and soft deletes for every poller cycle to submit the same message and failing.
        /// </summary>
        /// <param name="eventTrackingEvents">Check point events.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task RollBackEventCheckPoints(List<T> eventTrackingEvents)
        {
            if (eventTrackingEvents?.Any() == true)
            {
                using (var scope = _services.CreateScope())
                {
                    using (var transactionScope = new TransactionScope(
                        TransactionScopeOption.Required,
                        new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                        TransactionScopeAsyncFlowOption.Enabled))
                    {
                        _logger.LogError($"ICT Scheduler Event Poller Service Rolling Back,RollBackEventCheckPoints.");
                        var eventTrackingRepository = scope.ServiceProvider.GetRequiredService<IEventPublishCheckPointSqlRepository<TCP>>();
                        await eventTrackingRepository.DeleteEventCheckPoints(_eventPollerServiceSettings.DeleteEventPublishCheckPointQuery, eventTrackingEvents.Select(e => e.EventId).ToArray(), (byte)GetPollerProcess()).ConfigureAwait(false);
                        _logger.LogError($"ICT Scheduler Event Poller Service Rolled Back,RollBackEventCheckPoints.");
                        transactionScope.Complete();
                    }
                }
            }
        }

        /// <summary>
        /// HardDeleteEventCheckpoints.
        /// </summary>
        /// <param name="eventIds">eventIds.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task HardDeleteEventCheckpoints(int[] eventIds)
        {
            if (eventIds?.Any() == true)
            {
                using (var scope = _services.CreateScope())
                {
                    using (var transactionScope = new TransactionScope(
                        TransactionScopeOption.Required,
                        new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                        TransactionScopeAsyncFlowOption.Enabled))
                    {
                        var eventCheckpointRepository = scope.ServiceProvider.GetRequiredService<IEventPublishCheckPointSqlRepository<TCP>>();
                        await eventCheckpointRepository.HardDeleteEventCheckpoints(_eventPollerServiceSettings.EventPublishCheckPointRetentionQuery, eventIds).ConfigureAwait(false);
                        transactionScope.Complete();
                    }
                }
            }
        }

        /// <summary>
        /// HardDeleteEvents.
        /// </summary>
        /// <param name="eventIds">eventIds.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task HardDeleteEvents(int[] eventIds)
        {
            if (eventIds?.Any() == true)
            {
                using (var scope = _services.CreateScope())
                {
                    using (var transactionScope = new TransactionScope(
                        TransactionScopeOption.Required,
                        new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                        TransactionScopeAsyncFlowOption.Enabled))
                    {
                        var eventTrackingRepository = scope.ServiceProvider.GetRequiredService<IEventSqlServerRepository<T>>();
                        await eventTrackingRepository.HardDeleteEvents(_eventPollerServiceSettings.EventQueueRetentionQuery, eventIds).ConfigureAwait(false);
                        transactionScope.Complete();
                    }
                }
            }
        }

        /// <summary>
        /// Get UTC time.
        /// </summary>
        /// <param name="eventTracking">Event Tracking Event.</param>
        /// <returns>Return UTC time.</returns>
        protected string GetOffSetIdentifier(T eventTracking)
        {
            if (eventTracking is null)
            {
                throw new ArgumentNullException(nameof(eventTracking));
            }

            return TimeZoneInfo.ConvertTimeToUtc(eventTracking.DueDate.Value, TimeZoneInfo.Utc).ToString("o", CultureInfo.InvariantCulture) + "|" + eventTracking.EventId;
        }
    }
}
#pragma warning restore CA1715 // Identifiers should have correct prefix
#pragma warning restore CA1031 // Do not catch general exception types