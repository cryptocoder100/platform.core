#pragma warning disable CA1031 // Do not catch general exception types
namespace Exos.Platform.Persistence.EventListener
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Hosted service to process Event Messages.
    /// </summary>
    public abstract class EventListenerService : BackgroundService
    {
        private readonly ILogger<EventListenerService> _logger;
        private readonly EventListenerServiceSettings _options;
        private readonly IServiceProvider _services;
        private EventListenerServiceStatus _status;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventListenerService"/> class.
        /// </summary>
        /// <param name="services">IServiceProvider.</param>
        /// <param name="options">Configuration options to from settings file.</param>
        /// <param name="logger">Logger implementation.</param>
        protected EventListenerService(IServiceProvider services, IOptions<EventListenerServiceSettings> options, ILogger<EventListenerService> logger)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _status = new EventListenerServiceStatus();
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _status.State = EventListenerServiceState.Starting;
            _logger.LogDebug("Starting Event Listener Service");

            // Running
            _status.Started = DateTimeOffset.UtcNow;
            _status.State = EventListenerServiceState.Running;
            _logger.LogDebug("Running Event Listener Service");

            // Start the process at the exact minute
            DateTimeOffset currentDateTime = DateTime.UtcNow;
            int delaySeconds = 60 - currentDateTime.Second;
            _logger.LogDebug($"Event Listener Service will start in {delaySeconds} seconds.");
            await Task.Delay(new TimeSpan(0, 0, delaySeconds), stoppingToken).ConfigureAwait(false);

            // Polling Loop
            _logger.LogDebug($"Starting Event Listener Service.");
            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait for signal that additional work is ready to be processed.  In this case
                // we are using a simple timing loop
                try
                {
                    _status.PollingExecutions++;

                    // Read Messages
                    await ProcessMessages(stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Event Listener Service error {message}", ex.Message);
                }
            }

            // Stopping
            _status.Started = default(DateTimeOffset?);
            _status.State = EventListenerServiceState.Stopping;
            _logger.LogDebug("Stopping Event Listener Service");

            // Stopped
            _status.State = EventListenerServiceState.Stopped;
            _logger.LogDebug("Stopped Event Listener Service");
        }

        /// <summary>
        /// Abstract, implement will process messages.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Process event messages.</returns>
        protected abstract Task ProcessMessages(CancellationToken cancellationToken);
    }
}
#pragma warning restore CA1031 // Do not catch general exception types