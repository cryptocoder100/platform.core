#pragma warning disable CA1031 // Do not catch general exception types
namespace Exos.Platform.Persistence.EventPoller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Exos.Platform.ICTLibrary.Core;
    using Exos.Platform.ICTLibrary.Core.Model;
    using Exos.Platform.Persistence.EventTracking;
    using Exos.Platform.TenancyHelper.MultiTenancy;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// / Process persistent events using a HostedService and ICT library.
    /// </summary>
    /// <typeparam name="T">Entity that implements EventTrackingEntity class.</typeparam>
    public class ICTEventPollerHostedService<T> : BackgroundService where T : EventTrackingEntity, new()
    {
        private readonly ILogger<ICTEventPollerService<T>> _logger;
        private readonly EventPollerServiceSettings _options;
        private readonly IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="ICTEventPollerHostedService{T}"/> class.
        /// </summary>
        /// <param name="services">IServiceProvider implementation.</param>
        /// <param name="options">EventPollerServiceSettings from configuration file.</param>
        /// <param name="logger">Logger instance.</param>
        public ICTEventPollerHostedService(IServiceProvider services, IOptions<EventPollerServiceSettings> options, ILogger<ICTEventPollerService<T>> logger)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ICTEventPollerServiceStatus = new EventPollerServiceStatus();
        }

        /// <summary>
        /// Gets service status.
        /// </summary>
        public EventPollerServiceStatus ICTEventPollerServiceStatus { get; }

        /// <summary>
        /// Execute event processing.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token.</param>
        /// <returns>Events processed.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ICTEventPollerServiceStatus.EventPollerName = "ICT Event Poller Hosted Service.";
            ICTEventPollerServiceStatus.State = EventPollerServiceState.Starting;
            ICTEventPollerServiceStatus.Started = DateTimeOffset.UtcNow;
            ICTEventPollerServiceStatus.PollingExecutions = 0;

            _logger.LogDebug("Starting ICT Event Poller Hosted Service.");

            if (_options == null || _options.IsPollingEnabled == false)
            {
                _logger.LogDebug("Starting ICT Event Poller Hosted Service; polling is not enabled, exiting the poller.");
                ICTEventPollerServiceStatus.State = EventPollerServiceState.Disabled;
                return;
            }

            // Running
            ICTEventPollerServiceStatus.State = EventPollerServiceState.Running;
            _logger.LogDebug("Running ICT Event Poller Hosted Service.");

            // Polling Loop
            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait for signal that additional work is ready to be processed.  In this case
                // we are using a simple timing loop
                List<T> eventTrackingEvents = null;
                try
                {
                    ICTEventPollerServiceStatus.PollingExecutions++;
                    _logger.LogDebug("ICT Event Poller Hosted Service Reading Events from Event Table");
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

                        _logger.LogDebug($"ICT Event Poller Hosted Service {eventTrackingEvents.Count} Events to Process.");
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
                    _logger.LogError(ex, "ICT Event Poller Hosted Service {message}", ex.Message);
                    await RollBackEventStatus(eventTrackingEvents).ConfigureAwait(false);

                    // Clearing the list to not archive events with error.
                    eventTrackingEvents.Clear();
                }

                if (_options.IsArchivalEnabled)
                {
                    await ArchiveEvents(eventTrackingEvents).ConfigureAwait(false);
                }

                ICTEventPollerServiceStatus.LastExecution = DateTimeOffset.UtcNow;
                await Task.Delay(_options.EventPollingInterval, stoppingToken).ConfigureAwait(false);
            }

            // Stopping
            ICTEventPollerServiceStatus.State = EventPollerServiceState.Stopping;
            _logger.LogDebug("Stopping ICT Event Poller Hosted Service.");

            // Stopped
            ICTEventPollerServiceStatus.State = EventPollerServiceState.Stopped;
            _logger.LogDebug("Stopped ICT Event Poller Hosted Service.");
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
                        _logger.LogDebug($"ICT Event Poller Hosted Service Sending ICT message:{numberOfMessages} - {eventMessage}.");
                        await ictEventPublisher.PublishEvent(eventMessage).ConfigureAwait(false);
                        _logger.LogDebug($"ICT Event Poller Hosted Service Message {numberOfMessages} Sent to ICT.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"ICT Event Poller Hosted Service Error Sending to ICT Message: {numberOfMessages} - Event Id =  {eventTrackingEntity.EventId} ", ex.Message);
                        failedMessages++;
                        failedEvents.Add(eventTrackingEntity);
                    }
                }

                if (failedEvents.Any())
                {
                    // Remove failed messages from original list, we need to archive only successful messages
                    eventTrackingEvents.RemoveAll(i => failedEvents.Contains(i));
                }

                _logger.LogDebug($"ICT Event Poller Hosted Service Messages Sent to ICT: {numberOfMessages} - Failed Messages {failedMessages}.");
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
                    _logger.LogDebug($"ICT Event Poller Hosted Service Updating {eventTrackingEvents.Count} Events from Event Table.");
                    var eventTrackingRepository = scope.ServiceProvider.GetRequiredService<IEventSqlServerRepository<T>>();
                    await eventTrackingRepository.UpdateEventStatus(eventTrackingEvents, false, _options.UpdateQuery, _options.UpdateQueryBatchSize).ConfigureAwait(false);
                    _logger.LogDebug($"ICT Event Poller Hosted Service Updated {eventTrackingEvents.Count} Events from Event Table.");
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
                        _logger.LogError($"ICT Event Poller Hosted Service Rolling Back {eventTrackingEvents.Count} Events from Event Table.");
                        var eventTrackingRepository = scope.ServiceProvider.GetRequiredService<IEventSqlServerRepository<T>>();
                        await eventTrackingRepository.UpdateEventStatus(eventTrackingEvents, true, _options.UpdateQuery, _options.UpdateQueryBatchSize).ConfigureAwait(false);
                        _logger.LogError($"ICT Event Poller Hosted Service Rolled Back {eventTrackingEvents.Count} Events from Event Table.");
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
                        _logger.LogDebug($"ICT Event Poller Hosted Service Archiving {eventTrackingEvents.Count} Events from Event Table.");
                        var eventArchiveRepository = scope.ServiceProvider.GetRequiredService<IEventArchivalSqlServerRepository<T>>();
                        await eventArchiveRepository.ArchiveEvents(eventTrackingEvents).ConfigureAwait(false);
                        _logger.LogDebug($"ICT Event Poller Hosted Service Archived {eventTrackingEvents.Count} Events from Event Table.");
                        transactionScope.Complete();
                    }
                }
            }
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types