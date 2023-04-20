namespace Exos.MinimalPlatformService
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Exos.MinimalPlatformService.Models;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// An example background service that executes its business logic in a polling loop.
    /// </summary>
    public class BackgroundPollingService : BackgroundService
    {
        private readonly StatusModel _status;
        private readonly ILogger<BackgroundPollingService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundPollingService"/> class.
        /// </summary>
        /// <param name="status">Status Model.</param>
        /// <param name="logger">Logger Instance.</param>
        public BackgroundPollingService(StatusModel status, ILogger<BackgroundPollingService> logger)
        {
            _status = status ?? throw new ArgumentNullException(nameof(status));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Starting
            _logger.LogInformation("Background polling service starting");
            _status.PollingService.Status = Status.Starting;

            await Task.Delay(1000, stoppingToken).ConfigureAwait(false); // Simulate taking some time starting

            // Running
            _status.PollingService.Started = DateTimeOffset.UtcNow;
            _status.PollingService.Status = Status.Running;
            _logger.LogInformation("Background polling service running");

            // Polling loop
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Background polling service loop");
                _status.PollingService.Actions++;

                // Service logic goes here

                // Wait for signal that additional work is ready to be processed.  In this case
                // we are using a simple timing loop
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                }
            }

            // Stopping
            _logger.LogInformation("Background polling service stopping");
            _status.PollingService.Status = Status.Stopping;
            _status.PollingService.Started = default(DateTimeOffset?);

            await Task.Delay(1000, stoppingToken).ConfigureAwait(false);     // Simulate taking some time stopping

            // Stopped
            _status.PollingService.Status = Status.Stopped;
            _logger.LogInformation("Background polling service stopped");
        }
    }
}
