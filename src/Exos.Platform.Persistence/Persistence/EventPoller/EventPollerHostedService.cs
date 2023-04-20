#pragma warning disable CA1012 // Abstract types should not have constructors
#pragma warning disable CA1031 // Do not catch general exception types
namespace Exos.Platform.Persistence.EventPoller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Exos.Platform.Persistence.EventTracking;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Process persistent events using a HostedService.
    /// </summary>
    /// <typeparam name="T">Entity that implements EventTrackingEntity class.</typeparam>
    public abstract class EventPollerHostedService<T> : BackgroundService where T : EventTrackingEntity, new()
    {
        private readonly ILogger<EventPollerHostedService<T>> _logger;
        private readonly EventPollerServiceSettings _options;
        private readonly IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventPollerHostedService{T}"/> class.
        /// </summary>
        /// <param name="services">IServiceProvider implementation.</param>
        /// <param name="options">EventPollerServiceSettings from configuration file.</param>
        /// <param name="logger">Logger instance.</param>
        public EventPollerHostedService(IServiceProvider services, IOptions<EventPollerServiceSettings> options, ILogger<EventPollerHostedService<T>> logger)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            EventPollerServiceStatus = new EventPollerServiceStatus();
        }

        /// <summary>
        /// Gets service status.
        /// </summary>
        public EventPollerServiceStatus EventPollerServiceStatus { get; }

        /// <summary>
        /// Execute event processing.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token.</param>
        /// <returns>Events processed.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            EventPollerServiceStatus.EventPollerName = "Event Poller Hosted Service";
            EventPollerServiceStatus.State = EventPollerServiceState.Starting;
            EventPollerServiceStatus.Started = DateTimeOffset.UtcNow;
            EventPollerServiceStatus.PollingExecutions = 0;

            _logger.LogDebug("Starting Event Poller Hosted Service");

            if (_options == null || _options.IsPollingEnabled == false)
            {
                _logger.LogError("Starting Event Poller Hosted Service; polling is not enabled, exiting the poller");
                EventPollerServiceStatus.State = EventPollerServiceState.Disabled;
                return;
            }

            // Running
            EventPollerServiceStatus.State = EventPollerServiceState.Running;
            _logger.LogDebug("Running Event Poller Hosted Service");

            // Polling Loop
            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait for signal that additional work is ready to be processed.  In this case
                // we are using a simple timing loop
                List<T> eventTrackingEvents = null;
                try
                {
                    EventPollerServiceStatus.PollingExecutions++;
                    _logger.LogDebug("Event Poller Hosted Service Reading Events from Event Table");
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

                        _logger.LogDebug($"Event Poller Hosted Service {eventTrackingEvents.Count} Events to Process.");
                        if (eventTrackingEvents?.Any() == true)
                        {
                            var failedEvents = await SendEvents(eventTrackingEvents).ConfigureAwait(false);
                            if (failedEvents.Any())
                            {
                                await RollBackEventStatus(failedEvents).ConfigureAwait(false);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Event Poller Hosted Service error {message}", ex.Message);
                    await RollBackEventStatus(eventTrackingEvents).ConfigureAwait(false);
                    eventTrackingEvents.Clear();
                }

                if (_options.IsArchivalEnabled)
                {
                    await ArchiveEvents(eventTrackingEvents).ConfigureAwait(false);
                }

                EventPollerServiceStatus.LastExecution = DateTimeOffset.UtcNow;
                await Task.Delay(_options.EventPollingInterval, stoppingToken).ConfigureAwait(false);
            }

            // Stopping
            EventPollerServiceStatus.State = EventPollerServiceState.Stopping;
            _logger.LogDebug("Stopping Event Poller Hosted Service");

            // Stopped
            EventPollerServiceStatus.State = EventPollerServiceState.Stopped;
            _logger.LogDebug("Stopped Event Poller Hosted Service");
        }

        /// <summary>
        /// Implement this method to process the events.
        /// </summary>
        /// <param name="eventTrackingEvents">List of events.</param>
        /// <returns>Processed Events.</returns>
        protected abstract Task<List<T>> SendEvents(List<T> eventTrackingEvents);

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
                    _logger.LogDebug($"Event Poller Hosted Service Updating {eventTrackingEvents.Count} Events from Event Table.");
                    var eventTrackingRepository = scope.ServiceProvider.GetRequiredService<IEventSqlServerRepository<T>>();
                    await eventTrackingRepository.UpdateEventStatus(eventTrackingEvents, false, _options.UpdateQuery, _options.UpdateQueryBatchSize).ConfigureAwait(false);
                    _logger.LogDebug($"Event Poller Hosted Service Updated {eventTrackingEvents.Count} Events from Event Table.");
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
                        _logger.LogDebug($"Event Poller Hosted Service Rolling Back {eventTrackingEvents.Count} Events from Event Table.");
                        var eventTrackingRepository = scope.ServiceProvider.GetRequiredService<IEventSqlServerRepository<T>>();
                        await eventTrackingRepository.UpdateEventStatus(eventTrackingEvents, true, _options.UpdateQuery, _options.UpdateQueryBatchSize).ConfigureAwait(false);
                        _logger.LogDebug($"Event Poller Hosted Service Rolled Back {eventTrackingEvents.Count} Events from Event Table.");
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
                        _logger.LogDebug($"Event Poller Hosted Service Archiving {eventTrackingEvents.Count} Events from Event Table.");
                        var eventArchiveRepository = scope.ServiceProvider.GetRequiredService<IEventArchivalSqlServerRepository<T>>();
                        await eventArchiveRepository.ArchiveEvents(eventTrackingEvents).ConfigureAwait(false);
                        _logger.LogDebug($"Event Poller Hosted Service Archived {eventTrackingEvents.Count} Events from Event Table.");
                        transactionScope.Complete();
                    }
                }
            }
        }
    }
}
#pragma warning restore CA1012 // Abstract types should not have constructors
#pragma warning restore CA1031 // Do not catch general exception types