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

    /// <summary>
    /// Process persistent events sending events using ICT publisher.
    /// </summary>
    /// <typeparam name="T">Entity that implements EventTrackingEntity class.</typeparam>
    public class ICTEventPollerService<T> : IICTEventPollerService where T : EventTrackingEntity, new()
    {
        private readonly ILogger<ICTEventPollerService<T>> _logger;
        private readonly EventPollerServiceSettings _options;
        private readonly IServiceProvider _services;
        private EventPollerServiceStatus _status;

        /// <summary>
        /// Initializes a new instance of the <see cref="ICTEventPollerService{T}"/> class.
        /// </summary>
        /// <param name="services">IServiceProvider implementation.</param>
        /// <param name="options">EventPollerServiceSettings from configuration file.</param>
        /// <param name="logger">Logger instance.</param>
        public ICTEventPollerService(IServiceProvider services, IOptions<EventPollerServiceSettings> options, ILogger<ICTEventPollerService<T>> logger)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _status = new EventPollerServiceStatus
            {
                PollingExecutions = 0,
            };
        }

        /// <summary>
        /// Gets service status.
        /// </summary>
        /// <returns>Service status.</returns>
        public EventPollerServiceStatus ICTEventPollerServiceStatus()
        {
            return _status;
        }

        /// <summary>
        /// Execute event processing.
        /// </summary>
        /// <returns>Events processed.</returns>
        public async Task RunAsync()
        {
            _status.EventPollerName = "ICT Scheduler Event Poller Service.";
            _status.State = EventPollerServiceState.Starting;

            if (_options == null || _options.IsPollingEnabled == false)
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
            _logger.LogDebug("Running ICT Scheduler Event Poller Service.");

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
                        eventTrackingEvents = await eventTrackingRepository.FindEvents(_options.EventQuery).ConfigureAwait(false);
                        await UpdateEventStatus(eventTrackingEvents).ConfigureAwait(false);
                        transactionScope.Complete();
                    }

                    _logger.LogDebug($"ICT Scheduler Event Poller Service {eventTrackingEvents.Count} Events to Process.");
                    if (eventTrackingEvents?.Any() == true)
                    {
                        var failedEvents = await SendICTEvents(eventTrackingEvents).ConfigureAwait(false);
                        if (failedEvents.Any())
                        {
                            await RollBackEventStatus(failedEvents).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ICT Scheduler Event Poller Service error {message}", ex.Message);
                await RollBackEventStatus(eventTrackingEvents).ConfigureAwait(false);
                eventTrackingEvents.Clear();
            }

            if (_options.IsArchivalEnabled)
            {
                await ArchiveEvents(eventTrackingEvents).ConfigureAwait(false);
            }

            _status.LastExecution = DateTimeOffset.UtcNow;

            // Scheduled for next execution
            _status.State = EventPollerServiceState.Finished;
            _logger.LogDebug("Finished ICT Scheduler Event Poller Service.");
        }

        /// <summary>
        /// Process events publish them to ICT.
        /// </summary>
        /// <param name="eventTrackingEvents">List of events.</param>
        /// <returns>Processed Events.</returns>
        protected async Task<List<T>> SendICTEvents(List<T> eventTrackingEvents)
        {
            if (eventTrackingEvents == null)
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
                            var messageMetadata = JsonSerializer.Deserialize<Dictionary<string, string>>(eventTrackingEntity.Metadata);
                            foreach (var kv in messageMetadata)
                            {
                                additionalMessageHeaderData.Add(new KeyValuePair<string, string>(kv.Key, kv.Value));
                            }
                        }

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
                        try
                        {
                            _logger.LogDebug($"ICT Scheduler Event Poller Service Sending ICT message:{numberOfMessages} - {eventMessage}.");
                            await ictEventPublisher.PublishEvent(eventMessage).ConfigureAwait(false);
                            _logger.LogDebug($"ICT Scheduler Event Poller Service Message {numberOfMessages} Sent to ICT.");
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"ICT Scheduler Event Poller Service Error Sending to ICT Message: {numberOfMessages} - Event Id =  {eventTrackingEntity.EventId} ", e.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"ICT Scheduler Event Poller Service Error Sending to ICT Message: {numberOfMessages} - Event Id =  {eventTrackingEntity.EventId} ", ex.Message);
                        failedMessages++;
                        failedEvents.Add(eventTrackingEntity);
                    }
                }

                if (failedEvents.Any())
                {
                    // Remove failed messages from original list, we need to archive only successful messages
                    eventTrackingEvents.RemoveAll(i => failedEvents.Contains(i));
                }

                _logger.LogDebug($"ICT Scheduler Event Poller Service Messages Sent to ICT: {numberOfMessages} - Failed Messages {failedMessages}.");
            }

            return failedEvents;
        }

        /// <summary>
        /// Update the status of the events to inactive.
        /// </summary>
        /// <param name="eventTrackingEvents">List of events.</param>
        /// <returns>Updated events.</returns>
        protected async Task UpdateEventStatus(List<T> eventTrackingEvents)
        {
            if (eventTrackingEvents?.Any() == true)
            {
                using (var scope = _services.CreateScope())
                {
                    _logger.LogDebug($"ICT Scheduler Event Poller Service Updating {eventTrackingEvents.Count} Events from Event Table.");
                    var eventTrackingRepository = scope.ServiceProvider.GetRequiredService<IEventSqlServerRepository<T>>();
                    await eventTrackingRepository.UpdateEventStatus(eventTrackingEvents, false, _options.UpdateQuery, _options.UpdateQueryBatchSize).ConfigureAwait(false);
                    _logger.LogDebug($"ICT Scheduler Event Poller Service Updated {eventTrackingEvents.Count} Events from Event Table.");
                }
            }
        }

        /// <summary>
        /// Rollback the event processing.
        /// </summary>
        /// <param name="eventTrackingEvents">List of events.</param>
        /// <returns>Rolled-back events.</returns>
        protected async Task RollBackEventStatus(List<T> eventTrackingEvents)
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
                        _logger.LogError($"ICT Scheduler Event Poller Service Rolling Back {eventTrackingEvents.Count} Events from Event Table.");
                        var eventTrackingRepository = scope.ServiceProvider.GetRequiredService<IEventSqlServerRepository<T>>();
                        await eventTrackingRepository.UpdateEventStatus(eventTrackingEvents, true, _options.UpdateQuery, _options.UpdateQueryBatchSize).ConfigureAwait(false);
                        _logger.LogError($"ICT Scheduler Event Poller Service Rolled Back {eventTrackingEvents.Count} Events from Event Table.");
                        transactionScope.Complete();
                    }
                }
            }
        }

        /// <summary>
        /// Archive events.
        /// </summary>
        /// <param name="eventTrackingEvents">List of events.</param>
        /// <returns>Archived events.</returns>
        protected async Task ArchiveEvents(List<T> eventTrackingEvents)
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
                        _logger.LogDebug($"ICT Scheduler Event Poller Service Archiving {eventTrackingEvents.Count} Events from Event Table.");
                        var eventArchiveRepository = scope.ServiceProvider.GetRequiredService<IEventArchivalSqlServerRepository<T>>();
                        await eventArchiveRepository.ArchiveEvents(eventTrackingEvents).ConfigureAwait(false);
                        _logger.LogDebug($"ICT Scheduler Event Poller Service Archived {eventTrackingEvents.Count} Events from Event Table.");
                        transactionScope.Complete();
                    }
                }
            }
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types